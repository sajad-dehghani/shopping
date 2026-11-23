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

        // Card-to-card: the customer uploads the transfer receipt (multipart form, antiforgery validated automatically).
        app.MapPost("/order/{publicId:guid}/receipt", async (Guid publicId, [FromForm] ReceiptForm form, IFormFile? receipt,
                ReceiptService receipts, FlashMessages flash, CancellationToken ct) =>
            {
                if (receipt is null || receipt.Length == 0)
                {
                    flash.Set("تصویر رسید را انتخاب کنید.");
                    return Results.LocalRedirect($"/order/{publicId}#card-payment");
                }
                await using var stream = receipt.OpenReadStream();
                var result = await receipts.SubmitAsync(publicId, new ReceiptInput
                {
                    FullName = form.FullName ?? "",
                    Mobile = form.Mobile ?? "",
                    CardLast4 = form.CardLast4,
                    BankReference = form.BankReference,
                    Note = form.Note,
                }, stream, receipt.FileName, ct);
                flash.Set(result.Succeeded
                    ? "ok:رسید شما با موفقیت ارسال شد. پس از بررسی و تأیید مدیر، کد رهگیری روی همین صفحه نمایش داده می‌شود."
                    : result.Error!);
                return Results.LocalRedirect($"/order/{publicId}{(result.Succeeded ? "" : "#card-payment")}");
            })
            .ExcludeFromDescription();
    }

    public sealed class ReceiptForm
    {
        public string? FullName { get; set; }
        public string? Mobile { get; set; }
        public string? CardLast4 { get; set; }
        public string? BankReference { get; set; }
        public string? Note { get; set; }
    }

    public static void MapAdminReceiptEndpoints(this IEndpointRouteBuilder app)
    {
        // Receipt images are private: only admins can view them, and they are never cached by shared caches.
        app.MapGet("/admin/receipts/{id:int}/image", async (int id, ReceiptService receipts, HttpContext http, CancellationToken ct) =>
            {
                var file = await receipts.OpenImageAsync(id, ct);
                if (file is null) return Results.NotFound();
                http.Response.Headers.CacheControl = "private, no-store";
                http.Response.Headers["X-Content-Type-Options"] = "nosniff";
                return Results.Stream(file.Value.Content, file.Value.ContentType);
            })
            .RequireAuthorization(Policies.Admin)
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
