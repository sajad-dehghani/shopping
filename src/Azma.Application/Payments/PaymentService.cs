using Azma.Application.Abstractions;
using Azma.Application.Catalog;
using Azma.Application.Common;
using Azma.Domain.Catalog;
using Azma.Domain.Common;
using Azma.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Azma.Application.Payments;

public sealed record PaymentOutcome(Guid OrderPublicId, PaymentStatus Status, string? Message);

/// <summary>
/// Orchestrates online payment: starting a payment, handling the gateway callback and
/// re-checking unknown transactions. An order only becomes Paid after a server-side
/// Verify call confirms the payment — returning from the gateway is never enough.
/// </summary>
public sealed class PaymentService(
    IAppDbContextFactory dbFactory,
    IPaymentGateway gateway,
    ISiteClock clock,
    SiteSettingsService settingsService,
    IAuditLogger audit,
    ILogger<PaymentService> logger)
{
    /// <summary>A "Verifying" lock older than this is considered abandoned (e.g. process crashed).</summary>
    public static readonly TimeSpan StaleVerifyLock = TimeSpan.FromMinutes(2);

    /// <summary>Creates a gateway payment for the order and returns the URL to redirect the user to.</summary>
    public async Task<Result<string>> StartPaymentAsync(Guid orderPublicId, string callbackUrl, CancellationToken ct = default)
    {
        var settings = await settingsService.GetAsync(ct);
        if (!settings.PaymentEnabled) return Result<string>.Fail("پرداخت آنلاین موقتاً غیرفعال است. لطفاً با پشتیبانی تماس بگیرید.");
        if (!gateway.IsConfigured(settings.UseSandbox))
        {
            logger.LogError("Payment gateway is not configured (sandbox={Sandbox})", settings.UseSandbox);
            return Result<string>.Fail("درگاه پرداخت هنوز پیکربندی نشده است. لطفاً با پشتیبانی تماس بگیرید.");
        }

        await using var db = await dbFactory.CreateAsync(ct);
        var order = await db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Course)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Include(o => o.Enrollments)
            .Include(o => o.Transactions)
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.PublicId == orderPublicId, ct);
        if (order is null) return Result<string>.Fail("سفارش یافت نشد.");
        if (order.Status == OrderStatus.Paid) return Result<string>.Fail("این سفارش قبلاً پرداخت شده است.");
        if (!order.CanRetryPayment) return Result<string>.Fail("امکان پرداخت این سفارش وجود ندارد.");
        if (order.TotalAmount <= 0) return Result<string>.Fail("مبلغ سفارش نامعتبر است.");
        if (order.Transactions.Any(t => t.Status is PaymentStatus.Verifying or PaymentStatus.Succeeded))
            return Result<string>.Fail("پرداخت قبلی این سفارش در حال بررسی است. چند دقیقه دیگر دوباره تلاش کنید.");

        var now = clock.UtcNow;
        // Re-check availability every time (the item may have filled up or been unpublished since the order was created).
        var availability = await CheckAvailabilityAsync(db, order, now, ct);
        if (!availability.Succeeded) return Result<string>.Fail(availability.Error!);

        foreach (var e in order.Enrollments.Where(e => e.Status == EnrollmentStatus.PendingPayment))
            e.ReservedUntil = now.AddMinutes(settings.SeatReservationMinutes);
        if (order.Status == OrderStatus.PaymentFailed)
            order.ChangeStatus(OrderStatus.PendingPayment, now, "تلاش مجدد برای پرداخت");

        var amountRial = Money.TomanToRial(order.TotalAmount);
        var transaction = new PaymentTransaction
        {
            Gateway = gateway.Name,
            IsSandbox = settings.UseSandbox,
            Amount = order.TotalAmount,
            GatewayAmount = amountRial,
            Status = PaymentStatus.Pending,
            CreatedAt = now,
        };
        order.Transactions.Add(transaction);
        await db.SaveChangesAsync(ct);

        var description = SafeFormat(settings.PaymentDescription, order.OrderNumber);
        var response = await gateway.RequestAsync(new GatewayPaymentRequest(
            amountRial, description, callbackUrl, order.Mobile, order.Email, order.OrderNumber, settings.UseSandbox), ct);

        transaction.GatewayCode = response.Code;
        transaction.GatewayMessage = Truncate(response.Message, 500);
        transaction.UpdatedAt = clock.UtcNow;
        if (!response.Succeeded || string.IsNullOrEmpty(response.Authority))
        {
            transaction.Status = PaymentStatus.RequestFailed;
            await db.SaveChangesAsync(ct);
            logger.LogWarning("Payment request failed for order {OrderNumber}: {Code} {Message}", order.OrderNumber, response.Code, response.Message);
            return Result<string>.Fail("اتصال به درگاه پرداخت برقرار نشد. لطفاً دقایقی دیگر دوباره تلاش کنید.");
        }

        transaction.Authority = response.Authority;
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            // Unique index on Authority: never let one gateway session be attached to two transactions.
            logger.LogError(ex, "Could not store authority {Authority} for order {OrderNumber}", response.Authority, order.OrderNumber);
            transaction.Authority = null;
            transaction.Status = PaymentStatus.RequestFailed;
            transaction.GatewayMessage = "Authority could not be stored";
            await db.SaveChangesAsync(ct);
            return Result<string>.Fail("اتصال به درگاه پرداخت برقرار نشد. لطفاً دقایقی دیگر دوباره تلاش کنید.");
        }
        logger.LogInformation("Payment started for order {OrderNumber}, authority {Authority}", order.OrderNumber, response.Authority);
        return Result<string>.Ok(gateway.GetPaymentPageUrl(response.Authority, settings.UseSandbox));
    }

    /// <summary>Handles the user's return from the gateway (?Authority=...&amp;Status=OK|NOK).</summary>
    public async Task<Result<PaymentOutcome>> HandleCallbackAsync(string? authority, string? status, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(authority) || authority.Length > 100)
            return Result<PaymentOutcome>.Fail("اطلاعات بازگشتی از درگاه نامعتبر است.");

        int transactionId;
        await using (var db = await dbFactory.CreateAsync(ct))
        {
            transactionId = await db.PaymentTransactions.Where(t => t.Authority == authority).Select(t => t.Id).FirstOrDefaultAsync(ct);
        }
        if (transactionId == 0)
        {
            logger.LogWarning("Callback with unknown authority {Authority}", authority);
            return Result<PaymentOutcome>.Fail("تراکنش موردنظر یافت نشد.");
        }

        var callbackStatus = status is "OK" or "NOK" ? status : "INVALID";
        return Result<PaymentOutcome>.Ok(await VerifyTransactionAsync(transactionId, callbackStatus, null, ct));
    }

    /// <summary>Admin action: re-checks a transaction whose result is unknown/pending by calling Verify again.</summary>
    public async Task<Result<PaymentOutcome>> RecheckAsync(int transactionId, AdminActor actor, CancellationToken ct = default)
    {
        await using (var db = await dbFactory.CreateAsync(ct))
        {
            var t = await db.PaymentTransactions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == transactionId, ct);
            if (t is null) return Result<PaymentOutcome>.Fail("تراکنش یافت نشد.");
            if (string.IsNullOrEmpty(t.Authority)) return Result<PaymentOutcome>.Fail("این تراکنش شناسه Authority ندارد و قابل بررسی نیست.");
            if (t.Status is not (PaymentStatus.Pending or PaymentStatus.Unknown or PaymentStatus.Verifying))
                return Result<PaymentOutcome>.Fail("فقط تراکنش‌های در انتظار یا نامشخص قابل بررسی مجدد هستند.");
        }

        var outcome = await VerifyTransactionAsync(transactionId, null, actor, ct);
        await audit.LogAsync(actor, "RecheckPayment", nameof(PaymentTransaction), transactionId,
            $"نتیجه: {DisplayNames.Of(outcome.Status)} - {outcome.Message}", ct);
        return Result<PaymentOutcome>.Ok(outcome);
    }

    private async Task<PaymentOutcome> VerifyTransactionAsync(int transactionId, string? callbackStatus, AdminActor? actor, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateAsync(ct);
        var now = clock.UtcNow;
        var staleBefore = now - StaleVerifyLock;

        // Atomically claim the transaction. Only one request can move it to Verifying,
        // which prevents double processing when the callback is hit twice concurrently.
        var claimed = await db.PaymentTransactions
            .Where(t => t.Id == transactionId
                        && (t.Status == PaymentStatus.Pending || t.Status == PaymentStatus.Unknown
                            || (t.Status == PaymentStatus.Verifying && t.UpdatedAt < staleBefore)))
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.Status, PaymentStatus.Verifying)
                .SetProperty(t => t.UpdatedAt, now)
                .SetProperty(t => t.VerifyAttempts, t => t.VerifyAttempts + 1), ct);

        var transaction = await db.PaymentTransactions.Include(t => t.Order).FirstAsync(t => t.Id == transactionId, ct);
        var order = transaction.Order!;
        if (callbackStatus is not null && transaction.CallbackStatus is null)
            transaction.CallbackStatus = callbackStatus;

        if (claimed == 0)
        {
            // Already processed (or being processed right now by another request): just report the current state.
            await db.SaveChangesAsync(ct);
            return new PaymentOutcome(order.PublicId, transaction.Status, transaction.GatewayMessage);
        }

        GatewayVerifyResult result;
        if (transaction.GatewayAmount != Money.TomanToRial(order.TotalAmount) || transaction.Amount != order.TotalAmount)
        {
            logger.LogError("Amount mismatch for transaction {Id}: tx={TxAmount} order={OrderAmount}", transaction.Id, transaction.Amount, order.TotalAmount);
            result = new GatewayVerifyResult(GatewayVerifyOutcome.Failed, null, "مغایرت مبلغ تراکنش با مبلغ سفارش");
        }
        else
        {
            // Always verify, even when the callback says NOK: the server-side result is the source of truth.
            result = await gateway.VerifyAsync(transaction.Authority!, transaction.GatewayAmount, transaction.IsSandbox, ct);
        }

        now = clock.UtcNow;
        transaction.GatewayCode = result.Code;
        transaction.GatewayMessage = Truncate(result.Message, 500);
        transaction.UpdatedAt = now;
        var by = actor?.UserName;

        await using var dbTx = await db.Database.BeginTransactionAsync(ct);
        await db.Entry(order).Collection(o => o.Enrollments).LoadAsync(ct);
        await db.Entry(order).Collection(o => o.Items).LoadAsync(ct);
        await db.Entry(order).Collection(o => o.Transactions).LoadAsync(ct);

        switch (result.Outcome)
        {
            case GatewayVerifyOutcome.Verified or GatewayVerifyOutcome.AlreadyVerified:
                transaction.Status = PaymentStatus.Succeeded;
                transaction.RefId = result.RefId;
                transaction.CardPan = result.CardPan;
                transaction.VerifiedAt = now;
                await ApplySuccessAsync(db, order, transaction, now, by, ct);
                break;

            case GatewayVerifyOutcome.Failed:
                transaction.Status = callbackStatus == "NOK" ? PaymentStatus.Cancelled : PaymentStatus.Failed;
                var hasSuccess = order.Transactions.Any(t => t.Id != transaction.Id && t.Status == PaymentStatus.Succeeded);
                if (!hasSuccess && order.Status is OrderStatus.PendingPayment or OrderStatus.PaymentUnknown)
                {
                    order.ChangeStatus(OrderStatus.PaymentFailed, now,
                        transaction.Status == PaymentStatus.Cancelled ? "انصراف کاربر از پرداخت" : $"پرداخت ناموفق (کد {result.Code})", by);
                    foreach (var e in order.Enrollments.Where(e => e.Status == EnrollmentStatus.PendingPayment))
                        e.ReservedUntil = now; // release the seat
                }
                break;

            default:
                transaction.Status = PaymentStatus.Unknown;
                if (order.Status is OrderStatus.PendingPayment or OrderStatus.PaymentFailed)
                    order.ChangeStatus(OrderStatus.PaymentUnknown, now, "نتیجه پرداخت نامشخص - نیاز به بررسی مجدد", by);
                break;
        }

        await db.SaveChangesAsync(ct);
        await dbTx.CommitAsync(ct);

        logger.LogInformation("Transaction {Id} for order {OrderNumber} verified: {Outcome} (code {Code})",
            transaction.Id, order.OrderNumber, result.Outcome, result.Code);
        return new PaymentOutcome(order.PublicId, transaction.Status, transaction.GatewayMessage);
    }

    private async Task ApplySuccessAsync(IAppDbContext db, Order order, PaymentTransaction transaction, DateTime now, string? by, CancellationToken ct)
    {
        var otherSuccess = order.Transactions.FirstOrDefault(t => t.Id != transaction.Id && t.Status == PaymentStatus.Succeeded);
        if (order.Status == OrderStatus.Paid && otherSuccess is not null)
        {
            // The same order was paid twice (e.g. two tabs). Keep the record and flag it for a refund.
            AppendNote(order, $"⚠ پرداخت تکراری با کد پیگیری {transaction.RefId} ثبت شد؛ نیاز به بررسی و استرداد وجه.");
            logger.LogWarning("Duplicate successful payment for order {OrderNumber} (RefId {RefId})", order.OrderNumber, transaction.RefId);
            return;
        }
        if (order.Status == OrderStatus.Cancelled)
            AppendNote(order, "⚠ این سفارش پیش از تأیید پرداخت لغو شده بود؛ پرداخت موفق دریافت شد و سفارش دوباره فعال شد.");

        await OrderFulfillment.ApplyPaidAsync(db, order, now, $"تأیید پرداخت توسط درگاه (RefID: {transaction.RefId})", transaction.RefId, by, ct);
    }

    private Task<Result> CheckAvailabilityAsync(IAppDbContext db, Order order, DateTime now, CancellationToken ct)
        => OrderFulfillment.CheckAvailabilityAsync(db, order, now, clock.Today, ct);

    private static void AppendNote(Order order, string note)
        => order.AdminNote = string.IsNullOrEmpty(order.AdminNote) ? note : order.AdminNote + "\n" + note;

    private static string SafeFormat(string template, string orderNumber)
    {
        try { return Truncate(string.Format(template, orderNumber), 250)!; }
        catch (FormatException) { return $"پرداخت سفارش {orderNumber}"; }
    }

    private static string? Truncate(string? s, int max) => s is null || s.Length <= max ? s : s[..max];
}
