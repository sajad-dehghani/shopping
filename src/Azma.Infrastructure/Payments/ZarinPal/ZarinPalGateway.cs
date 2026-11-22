using System.Text.Json;
using System.Text.Json.Serialization;
using Azma.Application.Payments;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Azma.Infrastructure.Payments.ZarinPal;

/// <summary>
/// ZarinPal Payment Gateway v4 (REST/JSON).
///   request: POST {base}/pg/v4/payment/request.json
///   start:   GET  {base}/pg/StartPay/{authority}
///   verify:  POST {base}/pg/v4/payment/verify.json
/// Successful responses carry {"data":{"code":100,...},"errors":[]};
/// failures carry {"data":[],"errors":{"code":-9,"message":"..."}}.
/// </summary>
public sealed class ZarinPalGateway(HttpClient http, IOptions<ZarinPalOptions> options, ILogger<ZarinPalGateway> logger) : IPaymentGateway
{
    public const string HttpClientName = "zarinpal";
    private readonly ZarinPalOptions _options = options.Value;

    public string Name => "ZarinPal";

    public bool IsConfigured(bool useSandbox) => IsValidMerchantId(MerchantId(useSandbox));

    public async Task<GatewayRequestResult> RequestAsync(GatewayPaymentRequest request, CancellationToken cancellationToken = default)
    {
        var body = new RequestBody
        {
            MerchantId = MerchantId(request.UseSandbox)!,
            Amount = request.AmountRial,
            Currency = _options.Currency,
            Description = request.Description,
            CallbackUrl = request.CallbackUrl,
            Metadata = new Metadata { Mobile = request.Mobile, Email = request.Email, OrderId = request.OrderNumber },
        };

        var (response, error) = await PostAsync(BaseUrl(request.UseSandbox) + "/pg/v4/payment/request.json", body, cancellationToken);
        if (response is null) return GatewayRequestResult.Fail(null, error);

        if (response.Code == 100 && !string.IsNullOrEmpty(response.Authority))
            return GatewayRequestResult.Ok(response.Authority, 100, response.Message);

        logger.LogWarning("ZarinPal request rejected: code {Code}, message {Message}", response.Code, response.Message);
        return GatewayRequestResult.Fail(response.Code, response.Message);
    }

    public string GetPaymentPageUrl(string authority, bool useSandbox)
        => $"{BaseUrl(useSandbox)}/pg/StartPay/{Uri.EscapeDataString(authority)}";

    public async Task<GatewayVerifyResult> VerifyAsync(string authority, long amountRial, bool useSandbox, CancellationToken cancellationToken = default)
    {
        var body = new VerifyBody { MerchantId = MerchantId(useSandbox)!, Amount = amountRial, Authority = authority };
        var (response, error) = await PostAsync(BaseUrl(useSandbox) + "/pg/v4/payment/verify.json", body, cancellationToken);
        if (response is null) return new GatewayVerifyResult(GatewayVerifyOutcome.Unknown, null, error);

        return response.Code switch
        {
            100 => new GatewayVerifyResult(GatewayVerifyOutcome.Verified, 100, response.Message ?? "Verified", response.RefId, response.CardPan),
            101 => new GatewayVerifyResult(GatewayVerifyOutcome.AlreadyVerified, 101, response.Message ?? "Verified", response.RefId, response.CardPan),
            // Transient errors: retry later instead of failing the order.
            null or -12 or -52 => new GatewayVerifyResult(GatewayVerifyOutcome.Unknown, response.Code, response.Message ?? "Unexpected response"),
            _ => new GatewayVerifyResult(GatewayVerifyOutcome.Failed, response.Code, response.Message ?? DescribeCode(response.Code)),
        };
    }

    public static string DescribeCode(int? code) => code switch
    {
        100 => "عملیات موفق",
        101 => "تراکنش قبلاً تأیید شده است",
        -9 => "خطای اعتبارسنجی اطلاعات ارسالی",
        -10 => "آی‌پی یا مرچنت کد پذیرنده صحیح نیست",
        -11 => "مرچنت کد فعال نیست",
        -12 => "تلاش بیش از حد در یک بازه زمانی کوتاه",
        -15 => "درگاه پرداخت به حالت تعلیق درآمده است",
        -16 => "سطح تأیید پذیرنده پایین‌تر از سطح نقره‌ای است",
        -50 => "مبلغ پرداخت‌شده با مقدار مبلغ ارسالی در متد وریفای متفاوت است",
        -51 => "پرداخت ناموفق",
        -52 => "خطای غیرمنتظره؛ با پشتیبانی زرین‌پال تماس بگیرید",
        -53 => "پرداخت متعلق به این مرچنت کد نیست",
        -54 => "اتوریتی نامعتبر است",
        -55 => "تراکنش موردنظر یافت نشد",
        _ => "خطای نامشخص درگاه",
    };

    private async Task<(ZarinPalData? Data, string? Error)> PostAsync<TBody>(string url, TBody body, CancellationToken ct)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(Math.Max(5, _options.TimeoutSeconds)));
            // Buffered body with Content-Length (no chunked transfer encoding).
            using var content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), System.Text.Encoding.UTF8, "application/json");
            using var response = await http.PostAsync(url, content, cts.Token);
            var json = await response.Content.ReadAsStringAsync(cts.Token);
            var data = Parse(json);
            if (data is null)
            {
                logger.LogWarning("ZarinPal returned unparsable response ({Status}) from {Url}", (int)response.StatusCode, url);
                return (null, $"پاسخ نامعتبر از درگاه (HTTP {(int)response.StatusCode})");
            }
            return (data, null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            logger.LogWarning(ex, "ZarinPal call to {Url} failed", url);
            return (null, "عدم دسترسی به درگاه پرداخت (خطای شبکه یا پایان مهلت)");
        }
    }

    /// <summary>Normalizes both the success and the error response shapes.</summary>
    internal static ZarinPalData? Parse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return null;

            if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object && data.TryGetProperty("code", out var code))
            {
                return new ZarinPalData(
                    code.GetInt32(),
                    GetString(data, "message"),
                    GetString(data, "authority"),
                    GetString(data, "ref_id"),
                    GetString(data, "card_pan"));
            }
            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object && errors.TryGetProperty("code", out var errCode))
            {
                return new ZarinPalData(errCode.GetInt32(), GetString(errors, "message"), null, null, null);
            }
            return null;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException)
        {
            return null;
        }
    }

    private static string? GetString(JsonElement el, string name)
        => el.TryGetProperty(name, out var v)
            ? v.ValueKind switch
            {
                JsonValueKind.String => v.GetString(),
                JsonValueKind.Number => v.GetRawText(),
                _ => null,
            }
            : null;

    private string? MerchantId(bool sandbox) => sandbox ? _options.SandboxMerchantId : _options.MerchantId;
    private string BaseUrl(bool sandbox) => (sandbox ? _options.SandboxBaseUrl : _options.BaseUrl).TrimEnd('/');
    private static bool IsValidMerchantId(string? id) => !string.IsNullOrWhiteSpace(id) && Guid.TryParse(id, out _);

    internal sealed record ZarinPalData(int? Code, string? Message, string? Authority, string? RefId, string? CardPan);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private sealed class RequestBody
    {
        [JsonPropertyName("merchant_id")] public string MerchantId { get; init; } = "";
        [JsonPropertyName("amount")] public long Amount { get; init; }
        [JsonPropertyName("currency")] public string? Currency { get; init; }
        [JsonPropertyName("description")] public string Description { get; init; } = "";
        [JsonPropertyName("callback_url")] public string CallbackUrl { get; init; } = "";
        [JsonPropertyName("metadata")] public Metadata? Metadata { get; init; }
    }

    private sealed class Metadata
    {
        [JsonPropertyName("mobile")] public string? Mobile { get; init; }
        [JsonPropertyName("email")] public string? Email { get; init; }
        [JsonPropertyName("order_id")] public string? OrderId { get; init; }
    }

    private sealed class VerifyBody
    {
        [JsonPropertyName("merchant_id")] public string MerchantId { get; init; } = "";
        [JsonPropertyName("amount")] public long Amount { get; init; }
        [JsonPropertyName("authority")] public string Authority { get; init; } = "";
    }
}
