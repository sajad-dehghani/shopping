namespace Azma.Web.Infrastructure;

public static class SecurityHeaders
{
    /// <summary>Adds conservative security headers, including a CSP compatible with Blazor Server.</summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) => app.Use(async (ctx, next) =>
    {
        var h = ctx.Response.Headers;
        h["X-Content-Type-Options"] = "nosniff";
        h["X-Frame-Options"] = "DENY";
        h["Referrer-Policy"] = "strict-origin-when-cross-origin";
        h["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        h["Content-Security-Policy"] =
            "default-src 'self'; " +
            "img-src 'self' data: https://trustseal.enamad.ir; " +
            "style-src 'self' 'unsafe-inline'; " +
            "script-src 'self'; " +
            "font-src 'self'; " +
            "connect-src 'self' ws: wss:; " +
            "frame-ancestors 'none'; " +
            "base-uri 'self'; " +
            // the checkout form redirects to the gateway
            "form-action 'self' https://payment.zarinpal.com https://sandbox.zarinpal.com https://www.zarinpal.com";
        await next();
    });
}
