using Azma.Domain.Sales;

namespace Azma.Application.Admin;

public sealed class DashboardStats
{
    public int TotalEnrollments { get; init; }
    public int TodayEnrollments { get; init; }
    public int ActiveCourses { get; init; }
    public int Products { get; init; }
    public int PaidOrders { get; init; }
    public int PendingOrders { get; init; }
    public int UnknownPayments { get; init; }
    public long TotalPaidAmount { get; init; }
    public long TodayPaidAmount { get; init; }
    public int UnreadMessages { get; init; }
    public IReadOnlyList<EnrollmentRow> LatestEnrollments { get; init; } = [];
    public IReadOnlyList<TransactionRow> LatestTransactions { get; init; } = [];
}

public sealed class OrderFilter
{
    public string? Query { get; set; }
    public OrderStatus? Status { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class EnrollmentFilter
{
    public string? Query { get; set; }
    public int? CourseId { get; set; }
    public EnrollmentStatus? Status { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class TransactionFilter
{
    public string? Query { get; set; }
    public PaymentStatus? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class OrderRow
{
    public int Id { get; init; }
    public string OrderNumber { get; init; } = "";
    public string CustomerName { get; init; } = "";
    public string Mobile { get; init; } = "";
    public string Items { get; set; } = "";
    public long TotalAmount { get; init; }
    public OrderStatus Status { get; init; }
    public string? TrackingCode { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? PaidAt { get; init; }
}

public sealed class EnrollmentRow
{
    public int Id { get; init; }
    public int OrderId { get; init; }
    public string FullName { get; init; } = "";
    public string Mobile { get; init; } = "";
    public string? Email { get; init; }
    public string CourseTitle { get; init; } = "";
    public DateTime CreatedAt { get; init; }
    public long PaidAmount { get; init; }
    public EnrollmentStatus Status { get; init; }
    public OrderStatus OrderStatus { get; init; }
    public string OrderNumber { get; init; } = "";
    public string? TrackingCode { get; init; }
    public string? Notes { get; init; }
    public string? ExtraFieldsJson { get; init; }
}

public sealed class TransactionRow
{
    public int Id { get; init; }
    public int OrderId { get; init; }
    public string OrderNumber { get; init; } = "";
    public string CustomerName { get; init; } = "";
    public long Amount { get; init; }
    public PaymentStatus Status { get; init; }
    public string? Authority { get; init; }
    public string? RefId { get; init; }
    public string? CardPan { get; init; }
    public int? GatewayCode { get; init; }
    public bool IsSandbox { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? VerifiedAt { get; init; }
}
