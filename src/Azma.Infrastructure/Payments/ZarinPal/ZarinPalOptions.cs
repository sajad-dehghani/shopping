namespace Azma.Infrastructure.Payments.ZarinPal;

/// <summary>
/// ZarinPal settings. Bind from the "ZarinPal" configuration section.
/// Keep MerchantId out of source control: use environment variables (ZarinPal__MerchantId),
/// user-secrets in development, or your host's secret store.
/// </summary>
public sealed class ZarinPalOptions
{
    public const string SectionName = "ZarinPal";

    /// <summary>Production merchant ID (36-character UUID from the ZarinPal panel).</summary>
    public string? MerchantId { get; set; }

    /// <summary>Merchant ID used in sandbox mode. The sandbox accepts any well-formed UUID.</summary>
    public string? SandboxMerchantId { get; set; }

    public string BaseUrl { get; set; } = "https://payment.zarinpal.com";
    public string SandboxBaseUrl { get; set; } = "https://sandbox.zarinpal.com";

    /// <summary>Currency sent to the gateway. Amounts are always converted to this unit (IRR = Rial).</summary>
    public string Currency { get; set; } = "IRR";

    public int TimeoutSeconds { get; set; } = 20;
}
