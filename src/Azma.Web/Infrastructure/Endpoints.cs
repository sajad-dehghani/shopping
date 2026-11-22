using Azma.Application.Admin;
using Azma.Application.Common;
using Azma.Application.Payments;
using Azma.Domain.Sales;
using Microsoft.AspNetCore.Mvc;

namespace Azma.Web.Infrastructure;

public static class Endpoints
{
    public static void MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        // ZarinPal redirects the user here with ?Authority=...&Status=OK|NOK.
        // The status in the URL is NOT trusted: the payment is verified server-to-server.
        app.MapGet("/payment/zarinpal/callback", async (
                [FromQuery(Name = "Authority")] string? authority,
                [FromQuery(Name = "Status")] string? status,
                PaymentService payments, FlashMessages flash, CancellationToken ct) =>
            {
                var result = await payments.HandleCallbackAsync(authority, status, ct);
                if (!result.Succeeded)
                {
                    flash.Set(result.Error!);
                    return Results.LocalRedirect("/payment/error");
                }
                return Results.LocalRedirect($"/order/{result.Value!.OrderPublicId}");
            })
            .RequireRateLimiting("callback")
            .ExcludeFromDescription();

        // "Pay" / "Retry payment" button on the order page (antiforgery-protected form post).
        app.MapPost("/order/{publicId:guid}/pay", async (Guid publicId, PaymentService payments, SiteUrls urls, FlashMessages flash, CancellationToken ct) =>
            {
                var result = await payments.StartPaymentAsync(publicId, urls.PaymentCallback, ct);
                if (result.Succeeded) return Results.Redirect(result.Value!);
                flash.Set(result.Error!);
                return Results.LocalRedirect($"/order/{publicId}");
            })
            .ExcludeFromDescription();
    }

    public static void MapAdminExportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/export").RequireAuthorization(Policies.Admin);
        const string xlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        group.MapGet("/enrollments", async (string? q, int? courseId, EnrollmentStatus? status, string? from, string? to, SalesAdminService sales, CancellationToken ct) =>
        {
            var bytes = await sales.ExportEnrollmentsAsync(new EnrollmentFilter
            {
                Query = q, CourseId = courseId, Status = status, From = ParseDate(from), To = ParseDate(to),
            }, ct);
            return Results.File(bytes, xlsx, $"enrollments-{DateTime.UtcNow:yyyyMMdd-HHmm}.xlsx");
        });

        group.MapGet("/orders", async (string? q, OrderStatus? status, string? from, string? to, SalesAdminService sales, CancellationToken ct) =>
        {
            var bytes = await sales.ExportOrdersAsync(new OrderFilter { Query = q, Status = status, From = ParseDate(from), To = ParseDate(to) }, ct);
            return Results.File(bytes, xlsx, $"orders-{DateTime.UtcNow:yyyyMMdd-HHmm}.xlsx");
        });
    }

    private static DateOnly? ParseDate(string? jalali) => Persian.TryParseJalali(jalali, out var d) ? d : null;
}
