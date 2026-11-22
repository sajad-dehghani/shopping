using Azma.Domain.Catalog;
using Azma.Domain.Common;

namespace Azma.Domain.Sales;

public enum EnrollmentStatus
{
    PendingPayment = 0,
    Confirmed = 1,
    Cancelled = 2,
}

public class Enrollment : AuditableEntity
{
    public int CourseId { get; set; }
    public Course? Course { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    public int? OrderItemId { get; set; }
    public OrderItem? OrderItem { get; set; }

    public string FullName { get; set; } = "";
    public string Mobile { get; set; } = "";
    public string? Email { get; set; }
    public string? Notes { get; set; }
    /// <summary>JSON array of {label, value} for admin-defined fields.</summary>
    public string? ExtraFieldsJson { get; set; }

    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.PendingPayment;
    public DateTime? ConfirmedAt { get; set; }
    /// <summary>While unpaid, the seat is held until this UTC time.</summary>
    public DateTime? ReservedUntil { get; set; }
}
