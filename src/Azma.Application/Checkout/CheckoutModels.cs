using System.ComponentModel.DataAnnotations;
using Azma.Application.Common;

namespace Azma.Application.Checkout;

public class CustomerInput
{
    [Required(ErrorMessage = "نام و نام خانوادگی را وارد کنید.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "نام و نام خانوادگی باید بین ۳ تا ۱۰۰ حرف باشد.")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "شماره موبایل را وارد کنید.")]
    [StringLength(20)]
    public string Mobile { get; set; } = "";

    [OptionalEmail]
    [StringLength(150)]
    public string? Email { get; set; }

    [StringLength(1000, ErrorMessage = "توضیحات حداکثر ۱۰۰۰ حرف می‌تواند باشد.")]
    public string? Notes { get; set; }

    /// <summary>"online" (gateway) or "card" (card-to-card with receipt upload). Only affects where the user goes next.</summary>
    [StringLength(10)]
    public string? PaymentMethod { get; set; } = "online";

    public bool WantsCardToCard => string.Equals(PaymentMethod, "card", StringComparison.OrdinalIgnoreCase);
}

public sealed class CourseCheckoutInput : CustomerInput
{
    /// <summary>Values of admin-defined fields keyed by field id (as string, to bind from forms).</summary>
    public Dictionary<string, string?> Fields { get; set; } = [];
}

public sealed class ProductCheckoutInput : CustomerInput
{
    [Range(1, 20, ErrorMessage = "تعداد باید بین ۱ تا ۲۰ باشد.")]
    public int Quantity { get; set; } = 1;

    [StringLength(500)]
    public string? ShippingAddress { get; set; }

    [StringLength(20)]
    public string? PostalCode { get; set; }
}

public sealed record CheckoutResult(Guid OrderPublicId, string OrderNumber, long TotalAmount, bool IsFree);

public sealed record ExtraFieldValue(string Label, string Value);
