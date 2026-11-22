using Azma.Application.Abstractions;
using Azma.Application.Catalog;
using Azma.Application.Common;
using Azma.Domain.Catalog;
using Azma.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Azma.Application.Admin;

public sealed class CourseListRow
{
    public int Id { get; init; }
    public string Title { get; init; } = "";
    public string Slug { get; init; } = "";
    public string? CategoryName { get; init; }
    public string? InstructorName { get; init; }
    public DateOnly? StartDate { get; init; }
    public long Price { get; init; }
    public long? DiscountPrice { get; init; }
    public int? Capacity { get; init; }
    public int ConfirmedCount { get; init; }
    public int PendingCount { get; init; }
    public PublishStatus Status { get; init; }
    public bool IsFeatured { get; init; }
}

public sealed class ProductListRow
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string Slug { get; init; } = "";
    public string Sku { get; init; } = "";
    public string? CategoryName { get; init; }
    public ProductType ProductType { get; init; }
    public long Price { get; init; }
    public long? DiscountPrice { get; init; }
    public int? Stock { get; init; }
    public bool IsAvailable { get; init; }
    public PublishStatus Status { get; init; }
    public int PaidOrders { get; init; }
}

/// <summary>Create/update/delete for courses and products, with validation and audit logging.</summary>
public sealed class CatalogAdminService(IAppDbContextFactory dbFactory, ISiteClock clock, IAuditLogger audit, IFileStorage files)
{
    // ---------- Courses ----------

    public async Task<List<CourseListRow>> ListCoursesAsync(string? query, PublishStatus? status, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateAsync(ct);
        var now = clock.UtcNow;
        var q = db.Courses.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query)) { var t = Persian.NormalizeText(query); q = q.Where(c => c.Title.Contains(t)); }
        if (status is { } s) q = q.Where(c => c.Status == s);
        return await q.OrderBy(c => c.Status).ThenByDescending(c => c.Id).Select(c => new CourseListRow
        {
            Id = c.Id,
            Title = c.Title,
            Slug = c.Slug,
            CategoryName = c.Category != null ? c.Category.Name : null,
            InstructorName = c.Instructor != null ? c.Instructor.FullName : null,
            StartDate = c.StartDate,
            Price = c.Price,
            DiscountPrice = c.DiscountPrice,
            Capacity = c.Capacity,
            ConfirmedCount = c.Enrollments.Count(e => e.Status == Domain.Sales.EnrollmentStatus.Confirmed),
            PendingCount = c.Enrollments.Count(e => e.Status == Domain.Sales.EnrollmentStatus.PendingPayment && e.ReservedUntil > now),
            Status = c.Status,
            IsFeatured = c.IsFeatured,
        }).ToListAsync(ct);
    }

    public async Task<CourseEditModel?> GetCourseForEditAsync(int id, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateAsync(ct);
        var c = await db.Courses.AsNoTracking()
            .Include(x => x.Schedules).Include(x => x.RegistrationFields).Include(x => x.Images)
            .AsSplitQuery().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null) return null;
        return new CourseEditModel
        {
            Id = c.Id, Title = c.Title, Slug = c.Slug, CategoryId = c.CategoryId, InstructorId = c.InstructorId,
            ShortDescription = c.ShortDescription, Description = c.Description, Syllabus = c.Syllabus,
            Prerequisites = c.Prerequisites, RegistrationTerms = c.RegistrationTerms, CoverImagePath = c.CoverImagePath,
            Duration = c.Duration, StartDate = c.StartDate, EndDate = c.EndDate,
            RegistrationStartsOn = c.RegistrationStartsOn, RegistrationEndsOn = c.RegistrationEndsOn,
            DeliveryMode = c.DeliveryMode, Location = c.Location, OnlinePlatform = c.OnlinePlatform, SupportPhone = c.SupportPhone,
            Capacity = c.Capacity, Price = c.Price, DiscountPrice = c.DiscountPrice, Status = c.Status,
            IsFeatured = c.IsFeatured, AllowDuplicateEnrollment = c.AllowDuplicateEnrollment, SortOrder = c.SortOrder,
            Schedules = c.Schedules.OrderBy(s => Array.IndexOf(Persian.WeekDays, s.DayOfWeek)).ThenBy(s => s.StartTime)
                .Select(s => new ScheduleEditModel { Id = s.Id, DayOfWeek = s.DayOfWeek, StartTime = s.StartTime, EndTime = s.EndTime, Note = s.Note }).ToList(),
            Fields = c.RegistrationFields.OrderBy(f => f.SortOrder)
                .Select(f => new FieldEditModel { Id = f.Id, Label = f.Label, FieldType = f.FieldType, Options = f.Options, IsRequired = f.IsRequired }).ToList(),
            Images = c.Images.OrderBy(i => i.SortOrder).Select(i => new ImageEditModel { Id = i.Id, Path = i.Path, Caption = i.Caption }).ToList(),
        };
    }

    public async Task<Result<int>> SaveCourseAsync(CourseEditModel m, AdminActor actor, CancellationToken ct = default)
    {
        if (m.DiscountPrice is { } d && d >= m.Price && m.Price > 0) return Result<int>.Fail("قیمت با تخفیف باید کمتر از قیمت اصلی باشد.");
        if (m.StartDate is { } s && m.EndDate is { } e && e < s) return Result<int>.Fail("تاریخ پایان دوره نمی‌تواند قبل از تاریخ شروع باشد.");
        if (m.RegistrationStartsOn is { } rs && m.RegistrationEndsOn is { } re && re < rs) return Result<int>.Fail("پایان مهلت ثبت‌نام نمی‌تواند قبل از شروع آن باشد.");
        foreach (var sc in m.Schedules)
            if (sc.EndTime <= sc.StartTime) return Result<int>.Fail("در زمان‌بندی، ساعت پایان باید بعد از ساعت شروع باشد.");
        foreach (var f in m.Fields)
        {
            if (string.IsNullOrWhiteSpace(f.Label)) return Result<int>.Fail("عنوان همه فیلدهای فرم ثبت‌نام را وارد کنید.");
            if (f.FieldType == RegistrationFieldType.Select && string.IsNullOrWhiteSpace(f.Options))
                return Result<int>.Fail($"برای فیلد «{f.Label}» گزینه‌ها را (هر گزینه در یک خط) وارد کنید.");
        }

        await using var db = await dbFactory.CreateAsync(ct);
        var now = clock.UtcNow;
        Course course;
        if (m.Id == 0)
        {
            course = new Course { CreatedAt = now };
            db.Courses.Add(course);
        }
        else
        {
            course = await db.Courses.Include(x => x.Schedules).Include(x => x.RegistrationFields).Include(x => x.Images)
                .AsSplitQuery().FirstOrDefaultAsync(x => x.Id == m.Id, ct) ?? throw new InvalidOperationException("Course not found");
            course.UpdatedAt = now;
        }

        var slug = Persian.Slugify(string.IsNullOrWhiteSpace(m.Slug) ? m.Title : m.Slug);
        if (slug.Length == 0) return Result<int>.Fail("آدرس (اسلاگ) نامعتبر است.");
        if (await db.Courses.AnyAsync(x => x.Slug == slug && x.Id != m.Id, ct)) return Result<int>.Fail("این آدرس (اسلاگ) برای دوره دیگری استفاده شده است.");

        var removedFiles = new List<string?>();
        if (course.CoverImagePath != m.CoverImagePath) removedFiles.Add(course.CoverImagePath);

        course.Title = Persian.NormalizeText(m.Title);
        course.Slug = slug;
        course.CategoryId = m.CategoryId;
        course.InstructorId = m.InstructorId;
        course.ShortDescription = Clean(m.ShortDescription);
        course.Description = Clean(m.Description);
        course.Syllabus = Clean(m.Syllabus);
        course.Prerequisites = Clean(m.Prerequisites);
        course.RegistrationTerms = Clean(m.RegistrationTerms);
        course.CoverImagePath = m.CoverImagePath;
        course.Duration = Clean(m.Duration);
        course.StartDate = m.StartDate;
        course.EndDate = m.EndDate;
        course.RegistrationStartsOn = m.RegistrationStartsOn;
        course.RegistrationEndsOn = m.RegistrationEndsOn;
        course.DeliveryMode = m.DeliveryMode;
        course.Location = Clean(m.Location);
        course.OnlinePlatform = Clean(m.OnlinePlatform);
        course.SupportPhone = Clean(m.SupportPhone) is { } phone ? Persian.NormalizeDigits(phone) : null;
        course.Capacity = m.Capacity;
        course.Price = m.Price;
        course.DiscountPrice = m.DiscountPrice is > 0 ? m.DiscountPrice : null;
        course.Status = m.Status;
        course.IsFeatured = m.IsFeatured;
        course.AllowDuplicateEnrollment = m.AllowDuplicateEnrollment;
        course.SortOrder = m.SortOrder;

        SyncChildren(course.Schedules, m.Schedules, x => x.Id, x => x.Id, db,
            (entity, model) => { entity.DayOfWeek = model.DayOfWeek; entity.StartTime = model.StartTime; entity.EndTime = model.EndTime; entity.Note = Clean(model.Note); });
        var order = 0;
        SyncChildren(course.RegistrationFields, m.Fields, x => x.Id, x => x.Id, db,
            (entity, model) => { entity.Label = Persian.NormalizeText(model.Label); entity.FieldType = model.FieldType; entity.Options = Clean(model.Options); entity.IsRequired = model.IsRequired; entity.SortOrder = order++; });
        removedFiles.AddRange(course.Images.Where(i => m.Images.All(x => x.Id != i.Id)).Select(i => i.Path));
        var imgOrder = 0;
        SyncChildren(course.Images, m.Images, x => x.Id, x => x.Id, db,
            (entity, model) => { entity.Path = model.Path; entity.Caption = Clean(model.Caption); entity.SortOrder = imgOrder++; });

        await db.SaveChangesAsync(ct);
        foreach (var f in removedFiles) await files.DeleteAsync(f);
        await audit.LogAsync(actor, m.Id == 0 ? "CreateCourse" : "UpdateCourse", nameof(Course), course.Id,
            $"{course.Title}; price={course.Price}; discount={course.DiscountPrice}; capacity={course.Capacity}; status={course.Status}", ct);
        return Result<int>.Ok(course.Id);
    }

    public async Task<Result> DeleteCourseAsync(int id, AdminActor actor, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateAsync(ct);
        var course = await db.Courses.Include(c => c.Images).FirstOrDefaultAsync(c => c.Id == id, ct);
        if (course is null) return Result.Fail("دوره یافت نشد.");
        if (await db.OrderItems.AnyAsync(i => i.CourseId == id, ct) || await db.Enrollments.AnyAsync(e => e.CourseId == id, ct))
            return Result.Fail("برای این دوره سفارش یا ثبت‌نام ثبت شده و قابل حذف نیست. به جای حذف، وضعیت آن را «بایگانی» کنید.");
        var paths = course.Images.Select(i => i.Path).Append(course.CoverImagePath).ToList();
        db.Courses.Remove(course);
        await db.SaveChangesAsync(ct);
        foreach (var p in paths) await files.DeleteAsync(p);
        await audit.LogAsync(actor, "DeleteCourse", nameof(Course), id, course.Title, ct);
        return Result.Ok();
    }

    // ---------- Products ----------

    public async Task<List<ProductListRow>> ListProductsAsync(string? query, PublishStatus? status, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateAsync(ct);
        var q = db.Products.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query)) { var t = Persian.NormalizeText(query); q = q.Where(p => p.Name.Contains(t) || p.Sku.Contains(t)); }
        if (status is { } s) q = q.Where(p => p.Status == s);
        return await q.OrderBy(p => p.Status).ThenByDescending(p => p.Id).Select(p => new ProductListRow
        {
            Id = p.Id, Name = p.Name, Slug = p.Slug, Sku = p.Sku,
            CategoryName = p.Category != null ? p.Category.Name : null,
            ProductType = p.ProductType, Price = p.Price, DiscountPrice = p.DiscountPrice,
            Stock = p.Stock, IsAvailable = p.IsAvailable, Status = p.Status,
            PaidOrders = db.OrderItems.Count(i => i.ProductId == p.Id && i.Order!.Status == Domain.Sales.OrderStatus.Paid),
        }).ToListAsync(ct);
    }

    public async Task<ProductEditModel?> GetProductForEditAsync(int id, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateAsync(ct);
        var p = await db.Products.AsNoTracking().Include(x => x.Images).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return null;
        return new ProductEditModel
        {
            Id = p.Id, Name = p.Name, Slug = p.Slug, Sku = p.Sku, ShortDescription = p.ShortDescription, Description = p.Description,
            ImagePath = p.ImagePath, ProductType = p.ProductType, CategoryId = p.CategoryId, Price = p.Price, DiscountPrice = p.DiscountPrice,
            Stock = p.Stock, IsAvailable = p.IsAvailable, Status = p.Status, IsFeatured = p.IsFeatured, SortOrder = p.SortOrder,
            Images = p.Images.OrderBy(i => i.SortOrder).Select(i => new ImageEditModel { Id = i.Id, Path = i.Path }).ToList(),
        };
    }

    public async Task<Result<int>> SaveProductAsync(ProductEditModel m, AdminActor actor, CancellationToken ct = default)
    {
        if (m.DiscountPrice is { } d && d >= m.Price && m.Price > 0) return Result<int>.Fail("قیمت با تخفیف باید کمتر از قیمت اصلی باشد.");
        await using var db = await dbFactory.CreateAsync(ct);
        var now = clock.UtcNow;
        Product product;
        if (m.Id == 0)
        {
            product = new Product { CreatedAt = now };
            db.Products.Add(product);
        }
        else
        {
            product = await db.Products.Include(x => x.Images).FirstOrDefaultAsync(x => x.Id == m.Id, ct)
                      ?? throw new InvalidOperationException("Product not found");
            product.UpdatedAt = now;
        }

        var slug = Persian.Slugify(string.IsNullOrWhiteSpace(m.Slug) ? m.Name : m.Slug);
        if (slug.Length == 0) return Result<int>.Fail("آدرس (اسلاگ) نامعتبر است.");
        if (await db.Products.AnyAsync(x => x.Slug == slug && x.Id != m.Id, ct)) return Result<int>.Fail("این آدرس (اسلاگ) برای محصول دیگری استفاده شده است.");
        var sku = m.Sku.Trim().ToUpperInvariant();
        if (await db.Products.AnyAsync(x => x.Sku == sku && x.Id != m.Id, ct)) return Result<int>.Fail("این کد محصول قبلاً استفاده شده است.");

        var removedFiles = new List<string?>();
        if (product.ImagePath != m.ImagePath) removedFiles.Add(product.ImagePath);

        product.Name = Persian.NormalizeText(m.Name);
        product.Slug = slug;
        product.Sku = sku;
        product.ShortDescription = Clean(m.ShortDescription);
        product.Description = Clean(m.Description);
        product.ImagePath = m.ImagePath;
        product.ProductType = m.ProductType;
        product.CategoryId = m.CategoryId;
        product.Price = m.Price;
        product.DiscountPrice = m.DiscountPrice is > 0 ? m.DiscountPrice : null;
        product.Stock = m.Stock;
        product.IsAvailable = m.IsAvailable;
        product.Status = m.Status;
        product.IsFeatured = m.IsFeatured;
        product.SortOrder = m.SortOrder;

        removedFiles.AddRange(product.Images.Where(i => m.Images.All(x => x.Id != i.Id)).Select(i => i.Path));
        var imgOrder = 0;
        SyncChildren(product.Images, m.Images, x => x.Id, x => x.Id, db,
            (entity, model) => { entity.Path = model.Path; entity.SortOrder = imgOrder++; });

        await db.SaveChangesAsync(ct);
        foreach (var f in removedFiles) await files.DeleteAsync(f);
        await audit.LogAsync(actor, m.Id == 0 ? "CreateProduct" : "UpdateProduct", nameof(Product), product.Id,
            $"{product.Name}; sku={product.Sku}; price={product.Price}; discount={product.DiscountPrice}; stock={product.Stock}; status={product.Status}", ct);
        return Result<int>.Ok(product.Id);
    }

    public async Task<Result> DeleteProductAsync(int id, AdminActor actor, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateAsync(ct);
        var product = await db.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id, ct);
        if (product is null) return Result.Fail("محصول یافت نشد.");
        if (await db.OrderItems.AnyAsync(i => i.ProductId == id, ct))
            return Result.Fail("برای این محصول سفارش ثبت شده و قابل حذف نیست. به جای حذف، آن را «بایگانی» یا ناموجود کنید.");
        var paths = product.Images.Select(i => i.Path).Append(product.ImagePath).ToList();
        db.Products.Remove(product);
        await db.SaveChangesAsync(ct);
        foreach (var p in paths) await files.DeleteAsync(p);
        await audit.LogAsync(actor, "DeleteProduct", nameof(Product), id, product.Name, ct);
        return Result.Ok();
    }

    public async Task<List<OrderRow>> ProductOrdersAsync(int productId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateAsync(ct);
        return await db.OrderItems.AsNoTracking().Where(i => i.ProductId == productId).OrderByDescending(i => i.Id).Take(500)
            .Select(i => new OrderRow
            {
                Id = i.OrderId, OrderNumber = i.Order!.OrderNumber, CustomerName = i.Order.CustomerName, Mobile = i.Order.Mobile,
                Items = i.Quantity.ToString(), TotalAmount = i.TotalPrice, Status = i.Order.Status,
                TrackingCode = i.Order.TrackingCode, CreatedAt = i.Order.CreatedAt, PaidAt = i.Order.PaidAt,
            }).ToListAsync(ct);
    }

    // ---------- Lookups ----------

    public async Task<List<CategoryOption>> CourseCategoryOptionsAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateAsync(ct);
        return await db.CourseCategories.AsNoTracking().OrderBy(c => c.SortOrder).Select(c => new CategoryOption(c.Id, c.Name, c.Slug)).ToListAsync(ct);
    }

    public async Task<List<CategoryOption>> ProductCategoryOptionsAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateAsync(ct);
        return await db.ProductCategories.AsNoTracking().OrderBy(c => c.SortOrder).Select(c => new CategoryOption(c.Id, c.Name, c.Slug)).ToListAsync(ct);
    }

    public async Task<List<CategoryOption>> InstructorOptionsAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateAsync(ct);
        return await db.Instructors.AsNoTracking().OrderBy(i => i.SortOrder).Select(i => new CategoryOption(i.Id, i.FullName, "")).ToListAsync(ct);
    }

    public async Task<List<CategoryOption>> CourseOptionsAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateAsync(ct);
        return await db.Courses.AsNoTracking().OrderByDescending(c => c.Id).Select(c => new CategoryOption(c.Id, c.Title, c.Slug)).ToListAsync(ct);
    }

    private static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : Persian.NormalizeText(s);

    /// <summary>Updates an owned child collection in place: removes missing, updates existing, adds new.</summary>
    private static void SyncChildren<TEntity, TModel>(List<TEntity> entities, List<TModel> models, Func<TEntity, int> entityId, Func<TModel, int> modelId,
        IAppDbContext db, Action<TEntity, TModel> apply) where TEntity : class, new()
    {
        foreach (var removed in entities.Where(e => models.All(m => modelId(m) != entityId(e))).ToList())
        {
            entities.Remove(removed);
            db.Set<TEntity>().Remove(removed);
        }
        foreach (var model in models)
        {
            var entity = modelId(model) == 0 ? null : entities.FirstOrDefault(e => entityId(e) == modelId(model));
            if (entity is null)
            {
                entity = new TEntity();
                entities.Add(entity);
            }
            apply(entity, model);
        }
    }
}
