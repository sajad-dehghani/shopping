using Azma.Application.Abstractions;
using Azma.Application.Common;
using Azma.Domain.Auditing;
using Azma.Domain.Catalog;
using Azma.Domain.Common;
using Azma.Domain.Content;
using Microsoft.EntityFrameworkCore;

namespace Azma.Application.Admin;

/// <summary>Simple CRUD for small entities: categories, instructors, FAQ, features, contact messages, audit log.</summary>
public sealed class ContentAdminService(IAppDbContextFactory dbFactory, ISiteClock clock, IAuditLogger audit, IFileStorage files)
{
    public async Task<List<T>> ListAsync<T>(CancellationToken ct = default) where T : Entity
    {
        await using var db = await dbFactory.CreateAsync(ct);
        var q = db.Set<T>().AsNoTracking();
        return await (q switch
        {
            IQueryable<FaqItem> f => (IQueryable<T>)f.OrderBy(x => x.SortOrder).ThenBy(x => x.Id),
            IQueryable<FeatureItem> f => (IQueryable<T>)f.OrderBy(x => x.SortOrder).ThenBy(x => x.Id),
            IQueryable<Instructor> f => (IQueryable<T>)f.OrderBy(x => x.SortOrder).ThenBy(x => x.Id),
            IQueryable<CourseCategory> f => (IQueryable<T>)f.OrderBy(x => x.SortOrder).ThenBy(x => x.Id),
            IQueryable<ProductCategory> f => (IQueryable<T>)f.OrderBy(x => x.SortOrder).ThenBy(x => x.Id),
            _ => q.OrderByDescending(x => x.Id),
        }).ToListAsync(ct);
    }

    public async Task<Result> SaveAsync<T>(T entity, AdminActor actor, CancellationToken ct = default) where T : Entity
    {
        var validation = Validate(entity);
        if (validation is not null) return Result.Fail(validation);

        await using var db = await dbFactory.CreateAsync(ct);
        string? oldPhoto = null;
        switch (entity)
        {
            case CourseCategory c:
                c.Slug = Persian.Slugify(string.IsNullOrWhiteSpace(c.Slug) ? c.Name : c.Slug);
                if (await db.CourseCategories.AnyAsync(x => x.Slug == c.Slug && x.Id != c.Id, ct)) return Result.Fail("این اسلاگ تکراری است.");
                break;
            case ProductCategory p:
                p.Slug = Persian.Slugify(string.IsNullOrWhiteSpace(p.Slug) ? p.Name : p.Slug);
                if (await db.ProductCategories.AnyAsync(x => x.Slug == p.Slug && x.Id != p.Id, ct)) return Result.Fail("این اسلاگ تکراری است.");
                break;
            case Instructor i:
                if (i.Id == 0) i.CreatedAt = clock.UtcNow; else i.UpdatedAt = clock.UtcNow;
                if (i.Id != 0) oldPhoto = await db.Instructors.Where(x => x.Id == i.Id).Select(x => x.PhotoPath).FirstOrDefaultAsync(ct);
                break;
        }

        if (entity.Id == 0) db.Set<T>().Add(entity); else db.Set<T>().Update(entity);
        await db.SaveChangesAsync(ct);
        if (entity is Instructor ins && oldPhoto is not null && oldPhoto != ins.PhotoPath) await files.DeleteAsync(oldPhoto);
        await audit.LogAsync(actor, "Save" + typeof(T).Name, typeof(T).Name, entity.Id, Describe(entity), ct);
        return Result.Ok();
    }

    public async Task<Result> DeleteAsync<T>(int id, AdminActor actor, CancellationToken ct = default) where T : Entity
    {
        await using var db = await dbFactory.CreateAsync(ct);
        var entity = await db.Set<T>().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return Result.Fail("مورد یافت نشد.");
        db.Set<T>().Remove(entity);
        await db.SaveChangesAsync(ct);
        if (entity is Instructor i) await files.DeleteAsync(i.PhotoPath);
        await audit.LogAsync(actor, "Delete" + typeof(T).Name, typeof(T).Name, id, Describe(entity), ct);
        return Result.Ok();
    }

    public async Task MarkMessageReadAsync(int id, bool read, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateAsync(ct);
        await db.ContactMessages.Where(m => m.Id == id).ExecuteUpdateAsync(s => s.SetProperty(m => m.IsRead, read), ct);
    }

    public async Task<PagedResultLite<AdminAuditLog>> AuditLogsAsync(string? query, int page, int pageSize = 30, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateAsync(ct);
        var q = db.AdminAuditLogs.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query))
            q = q.Where(l => l.Action.Contains(query) || l.EntityType.Contains(query) || (l.UserName != null && l.UserName.Contains(query))
                             || (l.EntityId != null && l.EntityId.Contains(query)) || (l.Details != null && l.Details.Contains(query)));
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(l => l.Id).Skip((Math.Max(1, page) - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResultLite<AdminAuditLog>(items, total);
    }

    private static string? Validate(object entity) => entity switch
    {
        CourseCategory c when string.IsNullOrWhiteSpace(c.Name) => "نام دسته‌بندی الزامی است.",
        ProductCategory c when string.IsNullOrWhiteSpace(c.Name) => "نام دسته‌بندی الزامی است.",
        Instructor i when string.IsNullOrWhiteSpace(i.FullName) => "نام مدرس الزامی است.",
        FaqItem f when string.IsNullOrWhiteSpace(f.Question) || string.IsNullOrWhiteSpace(f.Answer) => "پرسش و پاسخ هر دو الزامی هستند.",
        FeatureItem f when string.IsNullOrWhiteSpace(f.Title) => "عنوان مزیت الزامی است.",
        FeatureItem f when !System.Text.RegularExpressions.Regex.IsMatch(f.Icon ?? "", "^bi-[a-z0-9-]+$") => "نام آیکون باید مانند bi-award باشد.",
        _ => null,
    };

    private static string Describe(object entity) => entity switch
    {
        CourseCategory c => c.Name,
        ProductCategory c => c.Name,
        Instructor i => i.FullName,
        FaqItem f => f.Question,
        FeatureItem f => f.Title,
        ContactMessage m => $"{m.FullName} {m.Mobile}",
        _ => entity.GetType().Name,
    };
}

public sealed record PagedResultLite<T>(IReadOnlyList<T> Items, int TotalCount);
