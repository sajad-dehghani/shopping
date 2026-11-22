using Azma.Application.Abstractions;
using Azma.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Azma.Application.Common;

/// <summary>Reads site settings with a short in-memory cache; invalidated on save.</summary>
public sealed class SiteSettingsService(IAppDbContextFactory dbFactory, IMemoryCache cache, IAuditLogger audit)
{
    private const string CacheKey = "site-settings";

    public async Task<SiteSettings> GetAsync(CancellationToken ct = default)
    {
        if (cache.TryGetValue(CacheKey, out SiteSettings? cached) && cached is not null) return cached;
        await using var db = await dbFactory.CreateAsync(ct);
        var settings = await db.SiteSettings.AsNoTracking().FirstOrDefaultAsync(s => s.Id == SiteSettings.SingletonId, ct)
                       ?? new SiteSettings();
        cache.Set(CacheKey, settings, TimeSpan.FromMinutes(10));
        return settings;
    }

    public async Task SaveAsync(SiteSettings input, AdminActor actor, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateAsync(ct);
        var entity = await db.SiteSettings.FirstOrDefaultAsync(s => s.Id == SiteSettings.SingletonId, ct);
        if (entity is null)
        {
            entity = new SiteSettings();
            db.SiteSettings.Add(entity);
        }
        var before = $"sandbox={entity.UseSandbox}, payment={entity.PaymentEnabled}";
        db.SiteSettings.Entry(entity).CurrentValues.SetValues(input);
        entity.Id = SiteSettings.SingletonId;
        entity.SeatReservationMinutes = Math.Clamp(entity.SeatReservationMinutes, 5, 1440);
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        cache.Remove(CacheKey);
        await audit.LogAsync(actor, "UpdateSettings", nameof(SiteSettings), entity.Id,
            $"{before} -> sandbox={entity.UseSandbox}, payment={entity.PaymentEnabled}", ct);
    }
}
