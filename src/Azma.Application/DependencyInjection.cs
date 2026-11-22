using Azma.Application.Admin;
using Azma.Application.Catalog;
using Azma.Application.Checkout;
using Azma.Application.Common;
using Azma.Application.Payments;
using Microsoft.Extensions.DependencyInjection;

namespace Azma.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMemoryCache();
        // Services are stateless and create their own short-lived DbContexts, so they are safe as scoped
        // services in both SSR requests and interactive Blazor circuits.
        services.AddScoped<SiteSettingsService>();
        services.AddScoped<CatalogService>();
        services.AddScoped<CheckoutService>();
        services.AddScoped<PaymentService>();
        services.AddScoped<ContactService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<SalesAdminService>();
        services.AddScoped<CatalogAdminService>();
        services.AddScoped<ContentAdminService>();
        return services;
    }
}
