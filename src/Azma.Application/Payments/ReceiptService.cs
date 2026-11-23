using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using Azma.Application.Abstractions;
using Azma.Application.Catalog;
using Azma.Application.Common;
using Azma.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Azma.Application.Payments;

public sealed class ReceiptInput
{
    [Required(ErrorMessage = "نام و نام خانوادگی واریزکننده را وارد کنید.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "نام و نام خانوادگی باید بین ۳ تا ۱۰۰ حرف باشد.")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "شماره موبایل را وارد کنید.")]
    [StringLength(20)]
    public string Mobile { get; set; } = "";

    [StringLength(4)]
    public string? CardLast4 { get; set; }

    [StringLength(50)]
    public string? BankReference { get; set; }

    [StringLength(500, ErrorMessage = "توضیحات حداکثر ۵۰۰ حرف می‌تواند باشد.")]
    public string? Note { get; set; }
}

public sealed record ReceiptRow(
    int Id, int OrderId, string OrderNumber, Guid OrderPublicId, string OrderCustomer, string PayerName, string PayerMobile,
    string? CardLast4, string? BankReference, string? CustomerNote, long Amount, long OrderTotal, ReceiptStatus Status,
    DateTime CreatedAt, string? ReviewedBy, DateTime? ReviewedAt, string? RejectReason, OrderStatus OrderStatus);

/// <summary>
/// Card-to-card payments: the customer uploads a transfer receipt, an admin reviews it, and only an
/// approval marks the order as Paid and issues the tracking code.
/// </summary>
public sealed class ReceiptService(
    IAppDbContextFactory dbFactory,
    IFileStorage files,
    ISiteClock clock,
    SiteSettingsService settingsService,
    IAuditLogger audit,
    ILogger<ReceiptService> logger)
{
    /// <summary>How long a course seat stays reserved while a receipt waits for review.</summary>
    public static readonly TimeSpan ReviewReservation = TimeSpan.FromHours(72);

    public async Task<Result> SubmitAsync(Guid orderPublicId, ReceiptInput input, Stream image, string fileName, CancellationToken ct = default)
    {
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(input, new ValidationContext(input), errors, true)) return Result.Fail(errors[0].ErrorMessage!);
        var mobile = Persian.NormalizeMobile(input.Mobile);
        if (mobile is null) return Result.Fail("شماره موبایل معتبر نیست (مثال: ۰۹۱۲۱۲۳۴۵۶۷).");
        var last4 = string.IsNullOrWhiteSpace(input.CardLast4) ? null : Persian.NormalizeDigits(input.CardLast4.Trim());
        if (last4 is not null && (last4.Length != 4 || !last4.All(char.IsAsciiDigit)))
            return Result.Fail("چهار رقم آخر کارت باید ۴ عدد باشد.");

        var settings = await settingsService.GetAsync(ct);
        if (!settings.CardToCardEnabled || string.IsNullOrWhiteSpace(settings.CardNumber))
            return Result.Fail("پرداخت کارت به کارت در حال حاضر فعال نیست.");

        await using var db = await dbFactory.CreateAsync(ct);
        var order = await db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Course)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Include(o => o.Enrollments)
            .Include(o => o.Receipts)
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.PublicId == orderPublicId, ct);
        if (order is null) return Result.Fail("سفارش یافت نشد.");
        if (order.Status == OrderStatus.AwaitingApproval) return Result.Fail("رسید این سفارش قبلاً ارسال شده و در حال بررسی است.");
        if (!order.CanSubmitReceipt) return Result.Fail("برای این سفارش امکان ارسال رسید وجود ندارد.");
        if (order.TotalAmount <= 0) return Result.Fail("مبلغ سفارش نامعتبر است.");

        var now = clock.UtcNow;
        var availability = await OrderFulfillment.CheckAvailabilityAsync(db, order, now, clock.Today, ct);
        if (!availability.Succeeded) return availability;

        string path;
        try
        {
            path = await files.SavePrivateImageAsync(image, fileName, "receipts", ct);
        }
        catch (FileValidationException ex)
        {
            return Result.Fail(ex.Message);
        }

        order.Receipts.Add(new PaymentReceipt
        {
            PayerName = Persian.NormalizeText(input.FullName),
            PayerMobile = mobile,
            CardLast4 = last4,
            BankReference = string.IsNullOrWhiteSpace(input.BankReference) ? null : Persian.NormalizeDigits(input.BankReference.Trim()),
            CustomerNote = string.IsNullOrWhiteSpace(input.Note) ? null : Persian.NormalizeText(input.Note),
            Amount = order.TotalAmount,
            ImagePath = path,
            CreatedAt = now,
        });
        order.ChangeStatus(OrderStatus.AwaitingApproval, now, "ارسال رسید کارت به کارت توسط مشتری");
        foreach (var e in order.Enrollments.Where(e => e.Status == EnrollmentStatus.PendingPayment))
            e.ReservedUntil = now + ReviewReservation;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Card-to-card receipt submitted for order {OrderNumber}", order.OrderNumber);
        return Result.Ok();
    }

    public async Task<PagedResult<ReceiptRow>> SearchAsync(ReceiptStatus? status, string? query, int page, int pageSize = 20, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateAsync(ct);
        var q = db.PaymentReceipts.AsNoTracking().AsQueryable();
        if (status is { } s) q = q.Where(r => r.Status == s);
        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = Persian.NormalizeDigits(query.Trim());
            q = q.Where(r => r.PayerName.Contains(term) || r.PayerMobile.Contains(term) || r.Order!.OrderNumber.Contains(term)
                             || r.Order.CustomerName.Contains(term) || (r.BankReference != null && r.BankReference.Contains(term)));
        }
        var total = await q.CountAsync(ct);
        page = Math.Max(1, page);
        var rows = await q.OrderBy(r => r.Status).ThenByDescending(r => r.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => new ReceiptRow(r.Id, r.OrderId, r.Order!.OrderNumber, r.Order.PublicId, r.Order.CustomerName, r.PayerName, r.PayerMobile,
                r.CardLast4, r.BankReference, r.CustomerNote, r.Amount, r.Order.TotalAmount, r.Status, r.CreatedAt, r.ReviewedBy, r.ReviewedAt,
                r.RejectReason, r.Order.Status))
            .ToListAsync(ct);
        return new PagedResult<ReceiptRow>(rows, total, page, pageSize);
    }

    public async Task<int> CountPendingAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateAsync(ct);
        return await db.PaymentReceipts.CountAsync(r => r.Status == ReceiptStatus.PendingReview, ct);
    }

    public async Task<(Stream Content, string ContentType)?> OpenImageAsync(int receiptId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateAsync(ct);
        var path = await db.PaymentReceipts.Where(r => r.Id == receiptId).Select(r => r.ImagePath).FirstOrDefaultAsync(ct);
        return files.OpenPrivate(path);
    }

    /// <summary>Admin approval: the order becomes Paid and receives a unique tracking code.</summary>
    public async Task<Result<string>> ApproveAsync(int receiptId, AdminActor actor, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var receipt = await db.PaymentReceipts.FirstOrDefaultAsync(r => r.Id == receiptId, ct);
        if (receipt is null) return Result<string>.Fail("رسید یافت نشد.");
        if (receipt.Status != ReceiptStatus.PendingReview) return Result<string>.Fail("این رسید قبلاً بررسی شده است.");

        var order = await db.Orders.Include(o => o.Items).Include(o => o.Enrollments).AsSplitQuery()
            .FirstAsync(o => o.Id == receipt.OrderId, ct);
        if (order.Status == OrderStatus.Paid) return Result<string>.Fail("این سفارش قبلاً پرداخت شده است؛ رسید را رد کنید یا وجه را مسترد نمایید.");
        if (order.Status == OrderStatus.Cancelled) return Result<string>.Fail("این سفارش لغو شده است.");

        var now = clock.UtcNow;
        var code = await NewTrackingCodeAsync(db, ct);
        receipt.Status = ReceiptStatus.Approved;
        receipt.ReviewedAt = now;
        receipt.ReviewedBy = actor.UserName;
        receipt.UpdatedAt = now;
        if (receipt.Amount != order.TotalAmount)
            OrderFulfillment.AppendNote(order, $"⚠ مبلغ رسید ({receipt.Amount}) با مبلغ سفارش ({order.TotalAmount}) متفاوت بود.");
        await OrderFulfillment.ApplyPaidAsync(db, order, now, "تأیید رسید کارت به کارت توسط مدیر", code, actor.UserName, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        await audit.LogAsync(actor, "ApproveReceipt", nameof(PaymentReceipt), receipt.Id, $"سفارش {order.OrderNumber} — کد رهگیری {code}", ct);
        logger.LogInformation("Receipt {ReceiptId} approved; order {OrderNumber} paid with tracking code {Code}", receipt.Id, order.OrderNumber, code);
        return Result<string>.Ok(code);
    }

    /// <summary>Admin rejection: the order goes back to "pending payment" so the customer can pay again or send a new receipt.</summary>
    public async Task<Result> RejectAsync(int receiptId, string reason, AdminActor actor, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 5) return Result.Fail("دلیل رد رسید را (حداقل ۵ حرف) بنویسید؛ این متن به مشتری نمایش داده می‌شود.");
        reason = reason.Trim();
        if (reason.Length > 500) reason = reason[..500];

        await using var db = await dbFactory.CreateAsync(ct);
        var receipt = await db.PaymentReceipts.FirstOrDefaultAsync(r => r.Id == receiptId, ct);
        if (receipt is null) return Result.Fail("رسید یافت نشد.");
        if (receipt.Status != ReceiptStatus.PendingReview) return Result.Fail("این رسید قبلاً بررسی شده است.");
        var order = await db.Orders.Include(o => o.Enrollments).FirstAsync(o => o.Id == receipt.OrderId, ct);

        var now = clock.UtcNow;
        receipt.Status = ReceiptStatus.Rejected;
        receipt.RejectReason = reason;
        receipt.ReviewedAt = now;
        receipt.ReviewedBy = actor.UserName;
        receipt.UpdatedAt = now;
        if (order.Status == OrderStatus.AwaitingApproval)
        {
            order.ChangeStatus(OrderStatus.PendingPayment, now, "رد رسید کارت به کارت: " + reason, actor.UserName);
            foreach (var e in order.Enrollments.Where(e => e.Status == EnrollmentStatus.PendingPayment))
                e.ReservedUntil = now; // release the seat; it is reserved again when the customer pays or re-sends
        }
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actor, "RejectReceipt", nameof(PaymentReceipt), receipt.Id, $"سفارش {order.OrderNumber}: {reason}", ct);
        return Result.Ok();
    }

    /// <summary>10-digit numeric tracking code, unique among orders.</summary>
    private static async Task<string> NewTrackingCodeAsync(IAppDbContext db, CancellationToken ct)
    {
        for (var i = 0; i < 10; i++)
        {
            var code = $"{RandomNumberGenerator.GetInt32(1, 10)}{RandomNumberGenerator.GetInt32(0, 1_000_000_000):000000000}";
            if (!await db.Orders.AnyAsync(o => o.TrackingCode == code || o.OrderNumber == code, ct)) return code;
        }
        return Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
    }
}
