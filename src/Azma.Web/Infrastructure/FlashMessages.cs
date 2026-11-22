namespace Azma.Web.Infrastructure;

/// <summary>One-time messages carried across a redirect in a short-lived, HttpOnly cookie.</summary>
public sealed class FlashMessages(IHttpContextAccessor accessor)
{
    private const string CookieName = "azma.flash";

    public void Set(string message)
    {
        var ctx = accessor.HttpContext;
        if (ctx is null || ctx.Response.HasStarted) return;
        ctx.Response.Cookies.Append(CookieName, Uri.EscapeDataString(message), new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = ctx.Request.IsHttps,
            MaxAge = TimeSpan.FromMinutes(2),
            IsEssential = true,
        });
    }

    public string? Take()
    {
        var ctx = accessor.HttpContext;
        if (ctx is null || !ctx.Request.Cookies.TryGetValue(CookieName, out var value)) return null;
        if (!ctx.Response.HasStarted) ctx.Response.Cookies.Delete(CookieName);
        var message = Uri.UnescapeDataString(value);
        return message.Length > 500 ? message[..500] : message;
    }
}
