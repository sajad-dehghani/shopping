namespace Azma.Application.Payments;

public sealed record GatewayPaymentRequest(
    long AmountRial,
    string Description,
    string CallbackUrl,
    string? Mobile,
    string? Email,
    string OrderNumber,
    bool UseSandbox);

public sealed record GatewayRequestResult(bool Succeeded, string? Authority, int? Code, string? Message)
{
    public static GatewayRequestResult Ok(string authority, int code, string? message) => new(true, authority, code, message);
    public static GatewayRequestResult Fail(int? code, string? message) => new(false, null, code, message);
}

public enum GatewayVerifyOutcome
{
    /// <summary>Code 100: payment verified now.</summary>
    Verified,
    /// <summary>Code 101: payment had already been verified before (still a success).</summary>
    AlreadyVerified,
    /// <summary>Definite failure: unpaid, cancelled, amount mismatch, invalid authority...</summary>
    Failed,
    /// <summary>Network error / timeout / unexpected response. Must be retried later.</summary>
    Unknown,
}

public sealed record GatewayVerifyResult(GatewayVerifyOutcome Outcome, int? Code, string? Message, string? RefId = null, string? CardPan = null)
{
    public bool IsSuccess => Outcome is GatewayVerifyOutcome.Verified or GatewayVerifyOutcome.AlreadyVerified;
}

/// <summary>Abstraction over an online payment gateway (ZarinPal implementation lives in Infrastructure).</summary>
public interface IPaymentGateway
{
    string Name { get; }
    /// <summary>True when a merchant ID is configured for the requested mode.</summary>
    bool IsConfigured(bool useSandbox);
    Task<GatewayRequestResult> RequestAsync(GatewayPaymentRequest request, CancellationToken cancellationToken = default);
    string GetPaymentPageUrl(string authority, bool useSandbox);
    Task<GatewayVerifyResult> VerifyAsync(string authority, long amountRial, bool useSandbox, CancellationToken cancellationToken = default);
}
