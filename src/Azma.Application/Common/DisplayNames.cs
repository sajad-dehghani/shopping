using Azma.Domain.Catalog;
using Azma.Domain.Common;
using Azma.Domain.Sales;

namespace Azma.Application.Common;

public static class DisplayNames
{
    public static string Of(DeliveryMode mode) => mode switch
    {
        DeliveryMode.InPerson => "حضوری",
        DeliveryMode.Online => "آنلاین",
        _ => "ترکیبی (حضوری و آنلاین)",
    };

    public static string Of(PublishStatus status) => status switch
    {
        PublishStatus.Draft => "پیش‌نویس",
        PublishStatus.Published => "منتشرشده",
        _ => "بایگانی/غیرفعال",
    };

    public static string Of(RegistrationState state) => state switch
    {
        RegistrationState.Open => "ثبت‌نام باز است",
        RegistrationState.NotStarted => "ثبت‌نام هنوز شروع نشده",
        RegistrationState.Closed => "مهلت ثبت‌نام به پایان رسیده",
        RegistrationState.Full => "ظرفیت تکمیل شده",
        _ => "غیرفعال",
    };

    public static string Of(OrderStatus status) => status switch
    {
        OrderStatus.PendingPayment => "در انتظار پرداخت",
        OrderStatus.Paid => "پرداخت‌شده",
        OrderStatus.PaymentFailed => "پرداخت ناموفق",
        OrderStatus.Cancelled => "لغوشده",
        OrderStatus.AwaitingApproval => "در انتظار تأیید رسید",
        _ => "نامشخص (نیاز به بررسی)",
    };

    public static string Of(PaymentStatus status) => status switch
    {
        PaymentStatus.Pending => "در انتظار بازگشت از درگاه",
        PaymentStatus.Verifying => "در حال اعتبارسنجی",
        PaymentStatus.Succeeded => "موفق",
        PaymentStatus.Failed => "ناموفق",
        PaymentStatus.Cancelled => "لغو توسط کاربر",
        PaymentStatus.Unknown => "نامشخص",
        _ => "خطا در اتصال به درگاه",
    };

    public static string Of(EnrollmentStatus status) => status switch
    {
        EnrollmentStatus.PendingPayment => "در انتظار پرداخت",
        EnrollmentStatus.Confirmed => "قطعی",
        _ => "لغوشده",
    };

    public static string Of(ProductType type) => type == ProductType.Digital ? "دیجیتال" : "فیزیکی";

    public static string Of(RegistrationFieldType type) => type switch
    {
        RegistrationFieldType.Text => "متن کوتاه",
        RegistrationFieldType.TextArea => "متن بلند",
        RegistrationFieldType.Number => "عدد",
        RegistrationFieldType.Select => "انتخاب از لیست",
        _ => "تیک (بله/خیر)",
    };

    public static string BadgeClass(OrderStatus status) => status switch
    {
        OrderStatus.Paid => "bg-success",
        OrderStatus.PendingPayment => "bg-warning text-dark",
        OrderStatus.PaymentUnknown => "bg-info text-dark",
        OrderStatus.Cancelled => "bg-secondary",
        OrderStatus.AwaitingApproval => "bg-primary",
        _ => "bg-danger",
    };

    public static string BadgeClass(PaymentStatus status) => status switch
    {
        PaymentStatus.Succeeded => "bg-success",
        PaymentStatus.Pending or PaymentStatus.Verifying => "bg-warning text-dark",
        PaymentStatus.Unknown => "bg-info text-dark",
        PaymentStatus.Cancelled => "bg-secondary",
        _ => "bg-danger",
    };

    public static string Of(Azma.Domain.Sales.ReceiptStatus status) => status switch
    {
        Azma.Domain.Sales.ReceiptStatus.PendingReview => "در انتظار بررسی",
        Azma.Domain.Sales.ReceiptStatus.Approved => "تأییدشده",
        _ => "ردشده",
    };

    public static string BadgeClass(Azma.Domain.Sales.ReceiptStatus status) => status switch
    {
        Azma.Domain.Sales.ReceiptStatus.PendingReview => "bg-warning text-dark",
        Azma.Domain.Sales.ReceiptStatus.Approved => "bg-success",
        _ => "bg-danger",
    };

    public static string Of(Azma.Domain.Support.TicketStatus status) => status switch
    {
        Azma.Domain.Support.TicketStatus.Open => "در انتظار پاسخ",
        Azma.Domain.Support.TicketStatus.Answered => "پاسخ داده شد",
        _ => "بسته‌شده",
    };

    public static string BadgeClass(Azma.Domain.Support.TicketStatus status) => status switch
    {
        Azma.Domain.Support.TicketStatus.Open => "bg-warning text-dark",
        Azma.Domain.Support.TicketStatus.Answered => "bg-success",
        _ => "bg-secondary",
    };
}
