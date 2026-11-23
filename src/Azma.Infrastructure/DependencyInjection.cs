using Azma.Application.Abstractions;
using Azma.Application.Payments;
using Azma.Infrastructure.Files;
using Azma.Infrastructure.Payments.ZarinPal;
using Azma.Infrastructure.Persistence;
using Azma.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Azma.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, string contentRootPath)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        // The factory also registers AppDbContext as a scoped service (used by ASP.NET Core Identity).
        // No automatic retry strategy: checkout and payment verification use explicit transactions,
        // and silently re-running financial operations is not desirable.
        services.AddDbContextFactory<AppDbContext>(o => o.UseSqlServer(connectionString,
            sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));
        services.AddSingleton<IAppDbContextFactory, AppDbContextFactory>();

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ISiteClock, IranSiteClock>();
        services.AddScoped<IAuditLogger, AuditLogger>();
        services.AddSingleton<IExcelExporter, ClosedXmlExcelExporter>();

        services.AddOptions<FileStorageOptions>()
            .Bind(configuration.GetSection(FileStorageOptions.SectionName))
            .PostConfigure(o =>
            {
                o.UploadsPath = Path.GetFullPath(Path.IsPathRooted(o.UploadsPath) ? o.UploadsPath : Path.Combine(contentRootPath, o.UploadsPath));
                o.PrivatePath = Path.GetFullPath(Path.IsPathRooted(o.PrivatePath) ? o.PrivatePath : Path.Combine(contentRootPath, o.PrivatePath));
            });
        services.AddSingleton<LocalFileStorage>();
        services.AddSingleton<IFileStorage>(sp => sp.GetRequiredService<LocalFileStorage>());

        services.AddOptions<ZarinPalOptions>().Bind(configuration.GetSection(ZarinPalOptions.SectionName));
        services.AddHttpClient<IPaymentGateway, ZarinPalGateway>(ZarinPalGateway.HttpClientName, (sp, client) =>
        {
            var o = sp.GetRequiredService<IOptions<ZarinPalOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(Math.Max(5, o.TimeoutSeconds) + 5);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Azma/1.0");
        });

        services.AddScoped<DbSeeder>();
        return services;
    }
}
