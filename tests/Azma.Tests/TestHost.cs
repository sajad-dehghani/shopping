using Azma.Application.Abstractions;
using Azma.Application.Common;
using Azma.Application.Payments;
using Azma.Domain.Catalog;
using Azma.Domain.Common;
using Azma.Domain.Content;
using Azma.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Azma.Tests;

/// <summary>In-memory SQLite database + fakes for exercising the application services end to end.</summary>
public sealed class TestHost : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    public FakeClock Clock { get; } = new();
    public FakeGateway Gateway { get; } = new();
    public IAppDbContextFactory DbFactory { get; }
    public SiteSettingsService Settings { get; }
    public IAuditLogger Audit { get; } = new NullAudit();

    public TestHost()
    {
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        using (var db = new AppDbContext(options)) db.Database.EnsureCreated();
        DbFactory = new Factory(options);
        Settings = new SiteSettingsService(DbFactory, new MemoryCache(new MemoryCacheOptions()), Audit);
        using var ctx = new AppDbContext(options);
        ctx.SiteSettings.Add(new SiteSettings { UseSandbox = true, PaymentEnabled = true, SeatReservationMinutes = 30 });
        ctx.SaveChanges();
    }

    public AppDbContext NewContext() => (AppDbContext)DbFactory.CreateAsync().GetAwaiter().GetResult();

    public Course AddCourse(long price = 1_000_000, long? discount = null, int? capacity = null, bool allowDuplicate = false)
    {
        using var db = NewContext();
        var c = new Course
        {
            Title = "دوره آزمایشی", Slug = "test-" + Guid.NewGuid().ToString("N")[..8], Price = price, DiscountPrice = discount,
            Capacity = capacity, Status = PublishStatus.Published, AllowDuplicateEnrollment = allowDuplicate, CreatedAt = Clock.UtcNow,
        };
        db.Courses.Add(c);
        db.SaveChanges();
        return c;
    }

    public Product AddProduct(long price = 200_000, int? stock = 10, ProductType type = ProductType.Digital)
    {
        using var db = NewContext();
        var p = new Product
        {
            Name = "محصول", Slug = "p-" + Guid.NewGuid().ToString("N")[..8], Sku = "SKU-" + Guid.NewGuid().ToString("N")[..6],
            Price = price, Stock = stock, ProductType = type, Status = PublishStatus.Published, CreatedAt = Clock.UtcNow,
        };
        db.Products.Add(p);
        db.SaveChanges();
        return p;
    }

    public void Dispose() => _connection.Dispose();

    private sealed class Factory(DbContextOptions<AppDbContext> options) : IAppDbContextFactory
    {
        public Task<IAppDbContext> CreateAsync(CancellationToken cancellationToken = default) => Task.FromResult<IAppDbContext>(new AppDbContext(options));
    }

    private sealed class NullAudit : IAuditLogger
    {
        public Task LogAsync(AdminActor actor, string action, string entityType, object? entityId, string? details = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}

public sealed class FakeClock : ISiteClock
{
    public DateTime UtcNow { get; set; } = new(2026, 11, 22, 8, 0, 0, DateTimeKind.Utc);
    public DateTime LocalNow => UtcNow.AddHours(3.5);
    public DateOnly Today => DateOnly.FromDateTime(LocalNow);
    public DateTime ToLocal(DateTime utc) => utc.AddHours(3.5);
    public DateTime StartOfLocalDayUtc(DateOnly date) => date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddHours(-3.5);
}

/// <summary>Scriptable gateway: records calls and returns configured results.</summary>
public sealed class FakeGateway : IPaymentGateway
{
    public List<GatewayPaymentRequest> Requests { get; } = [];
    public List<(string Authority, long Amount)> Verifies { get; } = [];
    public Func<string, long, GatewayVerifyResult> VerifyResult { get; set; } =
        (_, _) => new GatewayVerifyResult(GatewayVerifyOutcome.Verified, 100, "ok", "123456789", "6037****1234");
    public bool FailRequests { get; set; }

    public string Name => "Fake";
    public bool IsConfigured(bool useSandbox) => true;

    public Task<GatewayRequestResult> RequestAsync(GatewayPaymentRequest request, CancellationToken cancellationToken = default)
    {
        Requests.Add(request);
        return Task.FromResult(FailRequests
            ? GatewayRequestResult.Fail(-9, "validation")
            : GatewayRequestResult.Ok("A" + Guid.NewGuid().ToString("N")[..30], 100, "ok"));
    }

    public string GetPaymentPageUrl(string authority, bool useSandbox) => "https://gateway.test/StartPay/" + authority;

    public Task<GatewayVerifyResult> VerifyAsync(string authority, long amountRial, bool useSandbox, CancellationToken cancellationToken = default)
    {
        Verifies.Add((authority, amountRial));
        return Task.FromResult(VerifyResult(authority, amountRial));
    }
}
