using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Azma.Application;
using Azma.Application.Common;
using Azma.Infrastructure;
using Azma.Infrastructure.Files;
using Azma.Infrastructure.Identity;
using Azma.Infrastructure.Persistence;
using Azma.Web.Components;
using Azma.Web.Components.Account;
using Azma.Web.Infrastructure;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Persian culture for formatting; numbers are rendered with Persian digits by the Persian helper.
var culture = new CultureInfo("fa-IR");
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(o => o.DetailedErrors = builder.Environment.IsDevelopment());

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.ContentRootPath);

// ---------- Authentication (admin panel only) ----------
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();
builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/Account/Login";
    o.AccessDeniedPath = "/Account/AccessDenied";
    o.Cookie.Name = "azma.auth";
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Lax;
    o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    o.ExpireTimeSpan = TimeSpan.FromHours(8);
    o.SlidingExpiration = true;
});
builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager()
    .AddErrorDescriber<PersianIdentityErrorDescriber>()
    .AddDefaultTokenProviders();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Policies.Admin, p => p.RequireAuthenticatedUser().RequireRole(Roles.Admin));

// ---------- Rate limiting: all POSTs (forms, login, checkout) are limited per client IP ----------
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
    {
        if (!HttpMethods.IsPost(ctx.Request.Method)) return RateLimitPartition.GetNoLimiter("get");
        var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = builder.Configuration.GetValue("RateLimit:PostsPerMinute", 20),
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        });
    });
    o.AddFixedWindowLimiter("callback", l => { l.PermitLimit = 30; l.Window = TimeSpan.FromMinutes(1); l.QueueLimit = 0; });
});

builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // By default only a proxy on the same machine (loopback) is trusted. Add others via ReverseProxy:KnownProxies.
    foreach (var ip in builder.Configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? [])
        if (System.Net.IPAddress.TryParse(ip, out var addr)) o.KnownProxies.Add(addr);
});
// Persist Data Protection keys so auth cookies / antiforgery tokens survive restarts and work across instances.
var keysPath = builder.Configuration["DataProtection:KeysPath"] is { Length: > 0 } kp ? kp : Path.Combine(builder.Environment.ContentRootPath, "keys");
builder.Services.AddDataProtection()
    .SetApplicationName("Azma")
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<SiteUrls>();
builder.Services.AddScoped<FlashMessages>();
builder.Services.AddHealthChecks();

var app = builder.Build();

// ---------- Database migration + seed ----------
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
    await seeder.SeedAsync();

    // Admin recovery command: dotnet Azma.Web.dll admin <email> <password>
    // Creates the admin, or resets the password / unlocks an existing one, then exits.
    if (args.Length > 0 && args[0].Equals("admin", StringComparison.OrdinalIgnoreCase))
    {
        if (args.Length < 3)
        {
            Console.Error.WriteLine("Usage: dotnet Azma.Web.dll admin <email> <password>");
            return 1;
        }
        var error = await seeder.EnsureAdminAsync(args[1], args[2], resetPassword: true);
        Console.WriteLine(error is null ? $"Admin '{args[1]}' is ready. You can now log in at /Account/Login." : $"Failed: {error}");
        return error is null ? 0 : 1;
    }
}

app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
if (app.Configuration.GetValue("Https:Redirect", true)) app.UseHttpsRedirection();
app.UseSecurityHeaders();

// Sections switched off in admin settings behave as if they do not exist (404 page).
app.Use(async (ctx, next) =>
{
    var path = ctx.Request.Path;
    var isCourses = path.StartsWithSegments("/courses");
    var isShop = path.StartsWithSegments("/products");
    if (isCourses || isShop)
    {
        var s = await ctx.RequestServices.GetRequiredService<SiteSettingsService>().GetAsync(ctx.RequestAborted);
        if ((isCourses && !s.CoursesEnabled) || (isShop && !s.ShopEnabled))
        {
            ctx.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }
    }
    await next();
});

var uploads = app.Services.GetRequiredService<IOptions<FileStorageOptions>>().Value.UploadsPath;
Directory.CreateDirectory(uploads);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploads),
    RequestPath = "/uploads",
    ServeUnknownFileTypes = false,
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        ctx.Context.Response.Headers.CacheControl = "public,max-age=2592000";
    },
});

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapAdditionalIdentityEndpoints();
app.MapPaymentEndpoints();
app.MapAdminExportEndpoints();
app.MapAdminReceiptEndpoints();
app.MapHealthChecks("/health");

await app.RunAsync();
return 0;

public partial class Program;
