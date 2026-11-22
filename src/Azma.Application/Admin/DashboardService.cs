using Azma.Application.Abstractions;
using Azma.Domain.Common;
using Azma.Domain.Sales;
using Microsoft.EntityFrameworkCore;

namespace Azma.Application.Admin;

public sealed class DashboardService(IAppDbContextFactory dbFactory, ISiteClock clock)
{
    public async Task<DashboardStats> GetAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateAsync(ct);
        var todayStart = clock.StartOfLocalDayUtc(clock.Today);
        var today = clock.Today;

        return new DashboardStats
        {
            TotalEnrollments = await db.Enrollments.CountAsync(e => e.Status == EnrollmentStatus.Confirmed, ct),
            TodayEnrollments = await db.Enrollments.CountAsync(e => e.Status == EnrollmentStatus.Confirmed && e.ConfirmedAt >= todayStart, ct),
            ActiveCourses = await db.Courses.CountAsync(c => c.Status == PublishStatus.Published && (c.EndDate == null || c.EndDate >= today), ct),
            Products = await db.Products.CountAsync(p => p.Status == PublishStatus.Published, ct),
            PaidOrders = await db.Orders.CountAsync(o => o.Status == OrderStatus.Paid, ct),
            PendingOrders = await db.Orders.CountAsync(o => o.Status == OrderStatus.PendingPayment, ct),
            UnknownPayments = await db.Orders.CountAsync(o => o.Status == OrderStatus.PaymentUnknown, ct),
            TotalPaidAmount = await db.Orders.Where(o => o.Status == OrderStatus.Paid).SumAsync(o => (long?)o.TotalAmount, ct) ?? 0,
            TodayPaidAmount = await db.Orders.Where(o => o.Status == OrderStatus.Paid && o.PaidAt >= todayStart).SumAsync(o => (long?)o.TotalAmount, ct) ?? 0,
            UnreadMessages = await db.ContactMessages.CountAsync(m => !m.IsRead, ct),
            LatestEnrollments = await AdminQueries.EnrollmentRows(db.Enrollments.AsNoTracking().OrderByDescending(e => e.Id).Take(8)).ToListAsync(ct),
            LatestTransactions = await AdminQueries.TransactionRows(db.PaymentTransactions.AsNoTracking().OrderByDescending(t => t.Id).Take(8)).ToListAsync(ct),
        };
    }
}

internal static class AdminQueries
{
    public static IQueryable<EnrollmentRow> EnrollmentRows(IQueryable<Enrollment> q) => q.Select(e => new EnrollmentRow
    {
        Id = e.Id,
        OrderId = e.OrderId,
        FullName = e.FullName,
        Mobile = e.Mobile,
        Email = e.Email,
        CourseTitle = e.Course!.Title,
        CreatedAt = e.CreatedAt,
        PaidAmount = e.Order!.Status == OrderStatus.Paid ? (e.OrderItem != null ? e.OrderItem.TotalPrice : e.Order.TotalAmount) : 0,
        Status = e.Status,
        OrderStatus = e.Order!.Status,
        OrderNumber = e.Order.OrderNumber,
        TrackingCode = e.Order.TrackingCode,
        Notes = e.Notes,
        ExtraFieldsJson = e.ExtraFieldsJson,
    });

    public static IQueryable<TransactionRow> TransactionRows(IQueryable<PaymentTransaction> q) => q.Select(t => new TransactionRow
    {
        Id = t.Id,
        OrderId = t.OrderId,
        OrderNumber = t.Order!.OrderNumber,
        CustomerName = t.Order.CustomerName,
        Amount = t.Amount,
        Status = t.Status,
        Authority = t.Authority,
        RefId = t.RefId,
        CardPan = t.CardPan,
        GatewayCode = t.GatewayCode,
        IsSandbox = t.IsSandbox,
        CreatedAt = t.CreatedAt,
        VerifiedAt = t.VerifiedAt,
    });
}
