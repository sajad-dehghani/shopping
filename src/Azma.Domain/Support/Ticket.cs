using Azma.Domain.Common;
using Azma.Domain.Sales;

namespace Azma.Domain.Support;

public enum TicketStatus
{
    /// <summary>Waiting for the support team.</summary>
    Open = 0,
    /// <summary>Support replied; waiting for the customer.</summary>
    Answered = 1,
    Closed = 2,
}

/// <summary>A support ticket opened by a (guest) customer, linked to an order by its tracking code / order number.</summary>
public class Ticket : AuditableEntity
{
    public string TicketNumber { get; set; } = "";
    /// <summary>Unguessable id for the public ticket link.</summary>
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public int? OrderId { get; set; }
    public Order? Order { get; set; }
    public string FullName { get; set; } = "";
    public string Mobile { get; set; } = "";
    public string Subject { get; set; } = "";
    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public DateTime LastActivityAt { get; set; }
    public List<TicketMessage> Messages { get; set; } = [];
}

public class TicketMessage : Entity
{
    public int TicketId { get; set; }
    public Ticket? Ticket { get; set; }
    public string Body { get; set; } = "";
    public bool IsFromStaff { get; set; }
    public string? AuthorName { get; set; }
    public DateTime CreatedAt { get; set; }
}
