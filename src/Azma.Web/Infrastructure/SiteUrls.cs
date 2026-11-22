namespace Azma.Web.Infrastructure;

/// <summary>Builds absolute URLs. Use "Site:BaseUrl" in production so callbacks are correct behind proxies.</summary>
public sealed class SiteUrls(IConfiguration configuration, IHttpContextAccessor accessor)
{
    public string BaseUrl
    {
        get
        {
            var configured = configuration["Site:BaseUrl"];
            if (!string.IsNullOrWhiteSpace(configured)) return configured.TrimEnd('/');
            var req = accessor.HttpContext?.Request ?? throw new InvalidOperationException("No HTTP request available.");
            return $"{req.Scheme}://{req.Host}{req.PathBase}";
        }
    }

    public string PaymentCallback => BaseUrl + "/payment/zarinpal/callback";
}
