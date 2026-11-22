using Azma.Domain.Common;

namespace Azma.Domain.Sales;

public enum PaymentStatus
{
    /// <summary>Request sent to gateway, user redirected, awaiting callback.</summary>
    Pending = 0,
    /// <summary>A verify call is in progress (used as a lock against double processing).</summary>
    Verifying = 1,
    Succeeded = 2,
    Failed = 3,
    /// <summary>User cancelled at the gateway (Status=NOK).</summary>
    Cancelled = 4,
    /// <summary>Result unknown (network error / unexpected response); must be re-checked.</summary>
    Unknown = 5,
    /// <summary>Request to the gateway itself failed; no Authority issued.</summary>
    RequestFailed = 6,
}

public class PaymentTransaction : AuditableEntity
{
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    public string Gateway { get; set; } = "ZarinPal";
    public bool IsSandbox { get; set; }

    /// <summary>Amount in Toman (matches the order total at the time of the request).</summary>
    public long Amount { get; set; }
    /// <summary>Exact amount sent to the gateway, in Rial.</summary>
    public long GatewayAmount { get; set; }

    public string? Authority { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? RefId { get; set; }
    public string? CardPan { get; set; }
    public int? GatewayCode { get; set; }
    public string? GatewayMessage { get; set; }
    public string? CallbackStatus { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public int VerifyAttempts { get; set; }
}
