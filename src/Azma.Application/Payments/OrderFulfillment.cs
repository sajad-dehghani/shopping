using Azma.Application.Abstractions;
using Azma.Application.Catalog;
using Azma.Application.Common;
using Azma.Domain.Catalog;
using Azma.Domain.Sales;
using Microsoft.EntityFrameworkCore;

namespace Azma.Application.Payments;

/// <summary>
/// Shared "the order is now paid" logic used by both the online gateway and card-to-card approval:
/// status change, tracking code, confirming enrollments and decreasing stock.
/// Callers must have loaded <see cref="Order.Items"/> and <see cref="Order.Enrollments"/>.
/// </summary>
internal static class OrderFulfillment
{
    public static async Task ApplyPaidAsync(IAppDbContext db, Order order, DateTime now, string reason, string? trackingCode, string? by, CancellationToken ct)
    {
        order.ChangeStatus(OrderStatus.Paid, now, reason, by);
        order.PaidAt = now;
        order.TrackingCode = trackingCode;

        foreach (var e in order.Enrollments.Where(e => e.Status != EnrollmentStatus.Confirmed))
        {
            e.Status = EnrollmentStatus.Confirmed;
            e.ConfirmedAt = now;
            e.ReservedUntil = null;
            e.UpdatedAt = now;
        }

        foreach (var item in order.Items)
        {
            if (item.ItemType == OrderItemType.Product && item.ProductId is { } productId)
            {
                var product = await db.Products.FirstAsync(p => p.Id == productId, ct);
                if (product.Stock is { } stock)
                {
                    if (stock < item.Quantity)
                        AppendNote(order, $"⚠ موجودی «{product.Name}» برای این سفارش کافی نبود (موجودی {stock}).");
                    product.Stock = Math.Max(0, stock - item.Quantity);
                    product.UpdatedAt = now;
                }
            }
            else if (item.ItemType == OrderItemType.Course && item.CourseId is { } courseId)
            {
                var capacity = await db.Courses.Where(c => c.Id == courseId).Select(c => c.Capacity).FirstAsync(ct);
                if (capacity is { } cap)
                {
                    var confirmed = await db.Enrollments.CountAsync(e => e.CourseId == courseId && e.Status == EnrollmentStatus.Confirmed && e.OrderId != order.Id, ct);
                    if (confirmed + 1 > cap)
                        AppendNote(order, "⚠ ظرفیت دوره پیش از تأیید این پرداخت تکمیل شده بود.");
                }
            }
        }
    }

    /// <summary>Re-checks that the order's items can still be bought (course open / product in stock). Items must include Course/Product.</summary>
    public static async Task<Result> CheckAvailabilityAsync(IAppDbContext db, Order order, DateTime now, DateOnly today, CancellationToken ct)
    {
        foreach (var item in order.Items)
        {
            if (item.ItemType == OrderItemType.Course && item.Course is { } course)
            {
                var used = await db.Enrollments
                    .Where(e => e.CourseId == course.Id && e.OrderId != order.Id)
                    .CountAsync(SeatQueries.OccupiesSeat(now), ct);
                var state = course.GetRegistrationState(today, used);
                if (state != RegistrationState.Open) return Result.Fail($"امکان پرداخت وجود ندارد: {DisplayNames.Of(state)}.");
            }
            else if (item.ItemType == OrderItemType.Product && item.Product is { } product && !product.IsPurchasable(item.Quantity))
            {
                return Result.Fail($"محصول «{product.Name}» در حال حاضر موجود نیست.");
            }
        }
        return Result.Ok();
    }

    public static void AppendNote(Order order, string note)
        => order.AdminNote = string.IsNullOrEmpty(order.AdminNote) ? note : order.AdminNote + "\n" + note;
}
