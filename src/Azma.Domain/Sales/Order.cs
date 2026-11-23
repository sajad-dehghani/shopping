using Azma.Domain.Catalog;
using Azma.Domain.Common;

namespace Azma.Domain.Sales;

public enum OrderStatus
{
    PendingPayment = 0,
    Paid = 1,
    PaymentFailed = 2,
    Cancelled = 3,
    /// <summary>Gateway result could not be determined; needs re-check.</summary>
    PaymentUnknown = 4,
    /// <summary>A card-to-card receipt was uploaded and is waiting for admin approval.</summary>
    AwaitingApproval = 5,
}

public enum OrderItemType
{
    Course = 0,
    Product = 1,
}

public class Order : AuditableEntity
{
    public string OrderNumber { get; set; } = "";
    /// <summary>Unguessable id used in public links (order result / retry payment).</summary>
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string CustomerName { get; set; } = "";
    public string Mobile { get; set; } = "";
    public string? Email { get; set; }
    public string? CustomerNote { get; set; }
    public string? ShippingAddress { get; set; }
    public string? PostalCode { get; set; }

    /// <summary>Total in Toman, computed on the server at checkout.</summary>
    public long TotalAmount { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.PendingPayment;
    public DateTime? PaidAt { get; set; }
    /// <summary>Shown to the customer: ZarinPal RefID after a successful payment.</summary>
    public string? TrackingCode { get; set; }
    public string? AdminNote { get; set; }

    public List<OrderItem> Items { get; set; } = [];
    public List<Enrollment> Enrollments { get; set; } = [];
    public List<PaymentTransaction> Transactions { get; set; } = [];
    public List<OrderStatusHistory> StatusHistory { get; set; } = [];
    public List<PaymentReceipt> Receipts { get; set; } = [];

    public bool CanRetryPayment => Status is OrderStatus.PendingPayment or OrderStatus.PaymentFailed;
    /// <summary>Card-to-card receipts can be sent in the same states an online payment can be (re)started.</summary>
    public bool CanSubmitReceipt => CanRetryPayment;

    public void ChangeStatus(OrderStatus newStatus, DateTime utcNow, string reason, string? changedBy = null)
    {
        if (newStatus == Status) return;
        StatusHistory.Add(new OrderStatusHistory
        {
            FromStatus = Status,
            ToStatus = newStatus,
            Reason = reason,
            ChangedBy = changedBy,
            ChangedAt = utcNow,
        });
        Status = newStatus;
        UpdatedAt = utcNow;
    }
}

public class OrderItem : Entity
{
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    public OrderItemType ItemType { get; set; }
    public int? CourseId { get; set; }
    public Course? Course { get; set; }
    public int? ProductId { get; set; }
    public Product? Product { get; set; }
    /// <summary>Snapshot of the title at purchase time.</summary>
    public string Title { get; set; } = "";
    public long UnitPrice { get; set; }
    public long DiscountPerUnit { get; set; }
    public int Quantity { get; set; } = 1;
    public long TotalPrice { get; set; }
}

public class OrderStatusHistory : Entity
{
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    public OrderStatus FromStatus { get; set; }
    public OrderStatus ToStatus { get; set; }
    public string Reason { get; set; } = "";
    public string? ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; }
}
