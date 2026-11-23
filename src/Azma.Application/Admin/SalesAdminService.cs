using System.Text.Json;
using Azma.Application.Abstractions;
using Azma.Application.Catalog;
using Azma.Application.Checkout;
using Azma.Application.Common;
using Azma.Domain.Sales;
using Microsoft.EntityFrameworkCore;

namespace Azma.Application.Admin;

/// <summary>Orders, enrollments and transactions for the admin panel.</summary>
public sealed class SalesAdminService(IAppDbContextFactory dbFactory, ISiteClock clock, IAuditLogger audit, IExcelExporter excel)
{
    public async Task<PagedResult<OrderRow>> SearchOrdersAsync(OrderFilter f, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateAsync(ct);
        var q = FilterOrders(db.Orders.AsNoTracking(), f);
        var total = await q.CountAsync(ct);
        var rows = await ToOrderRows(q.OrderByDescending(o => o.Id).Skip((Math.Max(1, f.Page) - 1) * f.PageSize).Take(f.PageSize), db, ct);
        return new PagedResult<OrderRow>(rows, total, Math.Max(1, f.Page), f.PageSize);
    }

    public async Task<byte[]> ExportOrdersAsync(OrderFilter f, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateAsync(ct);
        var rows = await ToOrderRows(FilterOrders(db.Orders.AsNoTracking(), f).OrderByDescending(o => o.Id).Take(50_000), db, ct);
        return excel.Export("سفارش‌ها", new List<ExcelColumn<OrderRow>>
        {
            new("شماره سفارش", r => r.OrderNumber),
            new("نام مشتری", r => r.CustomerName),
            new("موبایل", r => r.Mobile),
            new("اقلام", r => r.Items),
            new("مبلغ (تومان)", r => r.TotalAmount),
            new("وضعیت", r => DisplayNames.Of(r.Status)),
            new("کد پیگیری", r => r.TrackingCode),
            new("تاریخ ثبت", r => Persian.ToJalali(clock.ToLocal(r.CreatedAt), true)),
            new("تاریخ پرداخت", r => r.PaidAt is { } p ? Persian.ToJalali(clock.ToLocal(p), true) : ""),
        }, rows);
    }

    public async Task<Order?> GetOrderAsync(int id, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateAsync(ct);
        return await db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.Enrollments).ThenInclude(e => e.Course)
            .Include(o => o.Transactions.OrderByDescending(t => t.Id))
            .Include(o => o.StatusHistory.OrderByDescending(h => h.Id))
            .Include(o => o.Receipts.OrderByDescending(r => r.Id))
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.Id == id, ct);
    }

    public async Task<Result> SaveAdminNoteAsync(int orderId, string? note, AdminActor actor, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateAsync(ct);
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (order is null) return Result.Fail("سفارش یافت نشد.");
        order.AdminNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim()[..Math.Min(note.Trim().Length, 2000)];
        order.UpdatedAt = clock.UtcNow;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actor, "UpdateOrderNote", nameof(Order), order.OrderNumber, order.AdminNote, ct);
        return Result.Ok();
    }

    /// <summary>Cancels an order (and its enrollments). A reason is mandatory and the action is audited.</summary>
    public async Task<Result> CancelOrderAsync(int orderId, string reason, AdminActor actor, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 5) return Result.Fail("دلیل لغو را (حداقل ۵ حرف) وارد کنید.");
        await using var db = await dbFactory.CreateAsync(ct);
        var order = await db.Orders.Include(o => o.Enrollments).Include(o => o.Items).Include(o => o.Transactions)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (order is null) return Result.Fail("سفارش یافت نشد.");
        if (order.Status == OrderStatus.Cancelled) return Result.Fail("این سفارش قبلاً لغو شده است.");
        if (order.Transactions.Any(t => t.Status is PaymentStatus.Verifying))
            return Result.Fail("یک تراکنش این سفارش در حال اعتبارسنجی است؛ کمی بعد دوباره تلاش کنید.");

        var now = clock.UtcNow;
        var wasPaid = order.Status == OrderStatus.Paid;
        order.ChangeStatus(OrderStatus.Cancelled, now, "لغو توسط مدیر: " + reason.Trim(), actor.UserName);
        foreach (var e in order.Enrollments)
        {
            e.Status = EnrollmentStatus.Cancelled;
            e.ReservedUntil = null;
            e.UpdatedAt = now;
        }
        if (wasPaid)
        {
            // Return stock of paid product items.
            foreach (var item in order.Items.Where(i => i.ProductId != null))
            {
                var product = await db.Products.FirstAsync(p => p.Id == item.ProductId, ct);
                if (product.Stock is { } stock) product.Stock = stock + item.Quantity;
            }
        }
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actor, "CancelOrder", nameof(Order), order.OrderNumber, $"paid={wasPaid}; reason={reason.Trim()}", ct);
        return Result.Ok();
    }

    public async Task<PagedResult<EnrollmentRow>> SearchEnrollmentsAsync(EnrollmentFilter f, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateAsync(ct);
        var q = FilterEnrollments(db.Enrollments.AsNoTracking(), f);
        var total = await q.CountAsync(ct);
        var rows = await AdminQueries.EnrollmentRows(q.OrderByDescending(e => e.Id).Skip((Math.Max(1, f.Page) - 1) * f.PageSize).Take(f.PageSize)).ToListAsync(ct);
        return new PagedResult<EnrollmentRow>(rows, total, Math.Max(1, f.Page), f.PageSize);
    }

    public async Task<byte[]> ExportEnrollmentsAsync(EnrollmentFilter f, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateAsync(ct);
        var rows = await AdminQueries.EnrollmentRows(FilterEnrollments(db.Enrollments.AsNoTracking(), f).OrderByDescending(e => e.Id).Take(50_000)).ToListAsync(ct);
        return excel.Export("ثبت‌نام‌ها", new List<ExcelColumn<EnrollmentRow>>
        {
            new("نام و نام خانوادگی", r => r.FullName),
            new("موبایل", r => r.Mobile),
            new("ایمیل", r => r.Email),
            new("دوره", r => r.CourseTitle),
            new("تاریخ ثبت‌نام", r => Persian.ToJalali(clock.ToLocal(r.CreatedAt), true)),
            new("مبلغ پرداختی (تومان)", r => r.PaidAmount),
            new("وضعیت ثبت‌نام", r => DisplayNames.Of(r.Status)),
            new("وضعیت پرداخت", r => DisplayNames.Of(r.OrderStatus)),
            new("شماره سفارش", r => r.OrderNumber),
            new("کد پیگیری", r => r.TrackingCode),
            new("توضیحات", r => r.Notes),
            new("اطلاعات تکمیلی", r => FormatExtraFields(r.ExtraFieldsJson)),
        }, rows);
    }

    public async Task<PagedResult<TransactionRow>> SearchTransactionsAsync(TransactionFilter f, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateAsync(ct);
        var q = db.PaymentTransactions.AsNoTracking();
        if (f.Status is { } s) q = q.Where(t => t.Status == s);
        if (!string.IsNullOrWhiteSpace(f.Query))
        {
            var term = Persian.NormalizeDigits(f.Query.Trim());
            q = q.Where(t => t.Order!.OrderNumber.Contains(term) || t.Order.Mobile.Contains(term)
                             || (t.Authority != null && t.Authority.Contains(term)) || (t.RefId != null && t.RefId.Contains(term)));
        }
        var total = await q.CountAsync(ct);
        var rows = await AdminQueries.TransactionRows(q.OrderByDescending(t => t.Id).Skip((Math.Max(1, f.Page) - 1) * f.PageSize).Take(f.PageSize)).ToListAsync(ct);
        return new PagedResult<TransactionRow>(rows, total, Math.Max(1, f.Page), f.PageSize);
    }

    public static string FormatExtraFields(string? json)
    {
        if (string.IsNullOrEmpty(json)) return "";
        try
        {
            var values = JsonSerializer.Deserialize<List<ExtraFieldValue>>(json) ?? [];
            return string.Join(" | ", values.Select(v => $"{v.Label}: {v.Value}"));
        }
        catch (JsonException)
        {
            return "";
        }
    }

    public static IReadOnlyList<ExtraFieldValue> ParseExtraFields(string? json)
    {
        if (string.IsNullOrEmpty(json)) return [];
        try { return JsonSerializer.Deserialize<List<ExtraFieldValue>>(json) ?? []; }
        catch (JsonException) { return []; }
    }

    private IQueryable<Order> FilterOrders(IQueryable<Order> q, OrderFilter f)
    {
        if (f.Status is { } s) q = q.Where(o => o.Status == s);
        if (!string.IsNullOrWhiteSpace(f.Query))
        {
            var term = Persian.NormalizeText(Persian.NormalizeDigits(f.Query));
            q = q.Where(o => o.OrderNumber.Contains(term) || o.Mobile.Contains(term) || o.CustomerName.Contains(term)
                             || (o.TrackingCode != null && o.TrackingCode.Contains(term)));
        }
        if (f.From is { } from) { var fromUtc = clock.StartOfLocalDayUtc(from); q = q.Where(o => o.CreatedAt >= fromUtc); }
        if (f.To is { } to) { var toUtc = clock.StartOfLocalDayUtc(to.AddDays(1)); q = q.Where(o => o.CreatedAt < toUtc); }
        return q;
    }

    private IQueryable<Enrollment> FilterEnrollments(IQueryable<Enrollment> q, EnrollmentFilter f)
    {
        if (f.CourseId is { } cid) q = q.Where(e => e.CourseId == cid);
        if (f.Status is { } s) q = q.Where(e => e.Status == s);
        if (!string.IsNullOrWhiteSpace(f.Query))
        {
            var term = Persian.NormalizeText(Persian.NormalizeDigits(f.Query));
            q = q.Where(e => e.FullName.Contains(term) || e.Mobile.Contains(term) || e.Order!.OrderNumber.Contains(term)
                             || (e.Order.TrackingCode != null && e.Order.TrackingCode.Contains(term)));
        }
        if (f.From is { } from) { var fromUtc = clock.StartOfLocalDayUtc(from); q = q.Where(e => e.CreatedAt >= fromUtc); }
        if (f.To is { } to) { var toUtc = clock.StartOfLocalDayUtc(to.AddDays(1)); q = q.Where(e => e.CreatedAt < toUtc); }
        return q;
    }

    private static async Task<List<OrderRow>> ToOrderRows(IQueryable<Order> q, IAppDbContext db, CancellationToken ct)
    {
        var rows = await q.Select(o => new OrderRow
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            CustomerName = o.CustomerName,
            Mobile = o.Mobile,
            TotalAmount = o.TotalAmount,
            Status = o.Status,
            TrackingCode = o.TrackingCode,
            CreatedAt = o.CreatedAt,
            PaidAt = o.PaidAt,
        }).ToListAsync(ct);
        var ids = rows.Select(r => r.Id).ToList();
        var items = await db.OrderItems.AsNoTracking().Where(i => ids.Contains(i.OrderId))
            .Select(i => new { i.OrderId, i.Title, i.Quantity }).ToListAsync(ct);
        foreach (var r in rows)
            r.Items = string.Join("، ", items.Where(i => i.OrderId == r.Id).Select(i => i.Quantity > 1 ? $"{i.Title} ×{i.Quantity}" : i.Title));
        return rows;
    }
}
