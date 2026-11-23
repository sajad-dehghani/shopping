using Azma.Domain.Common;

namespace Azma.Domain.Sales;

public enum ReceiptStatus
{
    PendingReview = 0,
    Approved = 1,
    Rejected = 2,
}

/// <summary>
/// A card-to-card (bank transfer) receipt uploaded by the customer. The order only becomes Paid
/// (and gets a tracking code) after an admin approves the receipt.
/// </summary>
public class PaymentReceipt : AuditableEntity
{
    public int OrderId { get; set; }
    public Order? Order { get; set; }

    /// <summary>Name and mobile of the person who made the transfer (required by the store).</summary>
    public string PayerName { get; set; } = "";
    public string PayerMobile { get; set; } = "";
    /// <summary>Last 4 digits of the card the money was sent from (optional).</summary>
    public string? CardLast4 { get; set; }
    /// <summary>Bank reference / trace number printed on the receipt (optional).</summary>
    public string? BankReference { get; set; }
    public string? CustomerNote { get; set; }

    /// <summary>Amount (Toman) the order totalled when the receipt was sent.</summary>
    public long Amount { get; set; }
    /// <summary>Private storage path of the receipt image (never served publicly).</summary>
    public string ImagePath { get; set; } = "";

    public ReceiptStatus Status { get; set; } = ReceiptStatus.PendingReview;
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RejectReason { get; set; }
}
