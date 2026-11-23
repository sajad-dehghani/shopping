using Azma.Application.Abstractions;
using Azma.Application.Common;
using Azma.Domain.Catalog;
using Azma.Domain.Common;
using Azma.Domain.Content;
using Microsoft.EntityFrameworkCore;

namespace Azma.Application.Catalog;

/// <summary>Read-side queries for the public website.</summary>
public sealed class CatalogService(IAppDbContextFactory dbFactory, ISiteClock clock, SiteSettingsService settingsService)
{
    public async Task<HomePageData> GetHomeAsync(CancellationToken ct = default)
    {
        var settings = await settingsService.GetAsync(ct);
        await using var db = await dbFactory.CreateAsync(ct);
        var today = clock.Today;

        var courseQuery = db.Courses.AsNoTracking()
            .Where(c => c.Status == PublishStatus.Published && (c.EndDate == null || c.EndDate >= today))
            .OrderByDescending(c => c.IsFeatured).ThenBy(c => c.SortOrder).ThenBy(c => c.StartDate);
        List<CourseCard> courses = settings.CoursesEnabled ? await ToCourseCards(courseQuery.Take(6)).ToListAsync(ct) : [];

        List<ProductCard> products = !settings.ShopEnabled ? [] : await ToProductCards(db.Products.AsNoTracking()
                .Where(p => p.Status == PublishStatus.Published)
                .OrderByDescending(p => p.IsFeatured).ThenBy(p => p.SortOrder).ThenByDescending(p => p.Id)
                .Take(8))
            .ToListAsync(ct);

        var instructors = settings.ShowInstructorsOnHome
            ? await db.Instructors.AsNoTracking().Where(i => i.IsActive && i.ShowOnHomePage).OrderBy(i => i.SortOrder).Take(8).ToListAsync(ct)
            : [];

        return new HomePageData
        {
            Settings = settings,
            Courses = Finish(courses, today),
            Products = products,
            Instructors = instructors,
            Features = await db.FeatureItems.AsNoTracking().Where(f => f.IsActive).OrderBy(f => f.SortOrder).ToListAsync(ct),
            Faqs = await db.FaqItems.AsNoTracking().Where(f => f.IsActive).OrderBy(f => f.SortOrder).ToListAsync(ct),
            Slides = await db.HeroSlides.AsNoTracking().Where(h => h.IsActive).OrderBy(h => h.SortOrder).ThenBy(h => h.Id).Take(10).ToListAsync(ct),
        };
    }

    public async Task<IReadOnlyList<FaqItem>> GetFaqsAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateAsync(ct);
        return await db.FaqItems.AsNoTracking().Where(f => f.IsActive).OrderBy(f => f.SortOrder).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<CategoryOption>> GetCourseCategoriesAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateAsync(ct);
        return await db.CourseCategories.AsNoTracking()
            .Where(c => c.Courses.Any(x => x.Status == PublishStatus.Published))
            .OrderBy(c => c.SortOrder).Select(c => new CategoryOption(c.Id, c.Name, c.Slug)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<CategoryOption>> GetProductCategoriesAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateAsync(ct);
        return await db.ProductCategories.AsNoTracking()
            .Where(c => c.Products.Any(x => x.Status == PublishStatus.Published))
            .OrderBy(c => c.SortOrder).Select(c => new CategoryOption(c.Id, c.Name, c.Slug)).ToListAsync(ct);
    }

    public async Task<PagedResult<CourseCard>> SearchCoursesAsync(CourseFilter filter, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateAsync(ct);
        var today = clock.Today;
        var now = clock.UtcNow;
        var q = db.Courses.AsNoTracking().Where(c => c.Status == PublishStatus.Published);

        if (!string.IsNullOrWhiteSpace(filter.Query))
        {
            var term = Persian.NormalizeText(filter.Query);
            q = q.Where(c => c.Title.Contains(term) || (c.ShortDescription != null && c.ShortDescription.Contains(term))
                             || (c.Instructor != null && c.Instructor.FullName.Contains(term)));
        }
        if (!string.IsNullOrWhiteSpace(filter.Category))
            q = q.Where(c => c.Category != null && c.Category.Slug == filter.Category);

        q = filter.When switch
        {
            CourseTimeFilter.Running => q.Where(c => c.StartDate != null && c.StartDate <= today && (c.EndDate == null || c.EndDate >= today)),
            CourseTimeFilter.Upcoming => q.Where(c => c.StartDate != null && c.StartDate > today),
            CourseTimeFilter.OpenForRegistration => q.Where(c =>
                (c.RegistrationStartsOn == null || c.RegistrationStartsOn <= today)
                && (c.RegistrationEndsOn == null || c.RegistrationEndsOn >= today)
                && (c.Capacity == null || c.Enrollments.AsQueryable().Count(SeatQueries.OccupiesSeat(now)) < c.Capacity)),
            _ => q,
        };

        var total = await q.CountAsync(ct);
        var page = Math.Max(1, filter.Page);
        var items = await ToCourseCards(q.OrderByDescending(c => c.IsFeatured).ThenBy(c => c.SortOrder).ThenByDescending(c => c.StartDate)
                .Skip((page - 1) * filter.PageSize).Take(filter.PageSize))
            .ToListAsync(ct);
        return new PagedResult<CourseCard>(Finish(items, today), total, page, filter.PageSize);
    }

    public async Task<CourseDetails?> GetCourseAsync(string slug, bool includeUnpublished = false, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateAsync(ct);
        var course = await db.Courses.AsNoTracking()
            .Include(c => c.Category)
            .Include(c => c.Instructor)
            .Include(c => c.Images.OrderBy(i => i.SortOrder))
            .Include(c => c.Schedules)
            .Include(c => c.RegistrationFields.OrderBy(f => f.SortOrder))
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Slug == slug, ct);
        if (course is null || (!includeUnpublished && course.Status != PublishStatus.Published)) return null;

        var used = await db.Enrollments.Where(e => e.CourseId == course.Id).CountAsync(SeatQueries.OccupiesSeat(clock.UtcNow), ct);
        return new CourseDetails
        {
            Course = course,
            UsedSeats = used,
            RemainingSeats = course.RemainingSeats(used),
            State = course.GetRegistrationState(clock.Today, used),
        };
    }

    public async Task<PagedResult<ProductCard>> SearchProductsAsync(ProductFilter filter, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateAsync(ct);
        var q = db.Products.AsNoTracking().Where(p => p.Status == PublishStatus.Published);
        if (!string.IsNullOrWhiteSpace(filter.Query))
        {
            var term = Persian.NormalizeText(filter.Query);
            q = q.Where(p => p.Name.Contains(term) || p.Sku.Contains(term) || (p.ShortDescription != null && p.ShortDescription.Contains(term)));
        }
        if (!string.IsNullOrWhiteSpace(filter.Category))
            q = q.Where(p => p.Category != null && p.Category.Slug == filter.Category);

        var total = await q.CountAsync(ct);
        var page = Math.Max(1, filter.Page);
        var items = await ToProductCards(q.OrderByDescending(p => p.IsFeatured).ThenBy(p => p.SortOrder).ThenByDescending(p => p.Id)
            .Skip((page - 1) * filter.PageSize).Take(filter.PageSize)).ToListAsync(ct);
        return new PagedResult<ProductCard>(items, total, page, filter.PageSize);
    }

    public async Task<Product?> GetProductAsync(string slug, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateAsync(ct);
        return await db.Products.AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Images.OrderBy(i => i.SortOrder))
            .FirstOrDefaultAsync(p => p.Slug == slug && p.Status == PublishStatus.Published, ct);
    }

    private IQueryable<CourseCard> ToCourseCards(IQueryable<Course> q)
    {
        var now = clock.UtcNow;
        return q.Select(c => new CourseCard
        {
            Id = c.Id,
            Title = c.Title,
            Slug = c.Slug,
            ShortDescription = c.ShortDescription,
            CoverImagePath = c.CoverImagePath,
            InstructorName = c.Instructor != null ? c.Instructor.FullName : null,
            CategoryName = c.Category != null ? c.Category.Name : null,
            StartDate = c.StartDate,
            EndDate = c.EndDate,
            DeliveryMode = c.DeliveryMode,
            Price = c.Price,
            DiscountPrice = c.DiscountPrice,
            Capacity = c.Capacity,
            IsFeatured = c.IsFeatured,
            Status = c.Status,
            RegistrationStartsOn = c.RegistrationStartsOn,
            RegistrationEndsOn = c.RegistrationEndsOn,
            UsedSeats = c.Enrollments.AsQueryable().Count(SeatQueries.OccupiesSeat(now)),
        });
    }

    private static IQueryable<ProductCard> ToProductCards(IQueryable<Product> q)
        => q.Select(p => new ProductCard
        {
            Id = p.Id,
            Name = p.Name,
            Slug = p.Slug,
            Sku = p.Sku,
            ShortDescription = p.ShortDescription,
            ImagePath = p.ImagePath,
            CategoryName = p.Category != null ? p.Category.Name : null,
            ProductType = p.ProductType,
            Price = p.Price,
            DiscountPrice = p.DiscountPrice,
            InStock = p.IsAvailable && (p.Stock == null || p.Stock > 0),
        });

    /// <summary>Computes registration state / remaining seats in memory (same rules as the domain entity).</summary>
    private static List<CourseCard> Finish(List<CourseCard> cards, DateOnly today)
    {
        foreach (var card in cards)
        {
            var probe = new Course
            {
                Status = card.Status,
                Capacity = card.Capacity,
                RegistrationStartsOn = card.RegistrationStartsOn,
                RegistrationEndsOn = card.RegistrationEndsOn,
            };
            card.State = probe.GetRegistrationState(today, card.UsedSeats);
            card.RemainingSeats = probe.RemainingSeats(card.UsedSeats);
        }
        return cards;
    }
}
