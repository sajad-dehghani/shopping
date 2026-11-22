using Azma.Domain.Catalog;
using Azma.Domain.Common;
using Azma.Domain.Content;
using Azma.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Azma.Infrastructure.Persistence;

/// <summary>
/// Applies migrations (optional), creates the Admin role, the first admin user (from configuration),
/// the settings row and — when enabled — sample data for testing.
/// </summary>
public sealed class DbSeeder(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IConfiguration configuration,
    ILogger<DbSeeder> logger)
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (configuration.GetValue("Database:AutoMigrate", true))
        {
            logger.LogInformation("Applying database migrations...");
            await db.Database.MigrateAsync(ct);
        }

        if (!await roleManager.RoleExistsAsync(Roles.Admin))
            await roleManager.CreateAsync(new IdentityRole(Roles.Admin));

        await SeedAdminAsync();

        if (!await db.SiteSettings.AnyAsync(ct))
        {
            db.SiteSettings.Add(DefaultSettings());
            await db.SaveChangesAsync(ct);
        }

        if (configuration.GetValue("Database:SeedSampleData", false) && !await db.Courses.AnyAsync(ct))
        {
            logger.LogInformation("Seeding sample data...");
            await SeedSampleDataAsync(ct);
        }
    }

    private async Task SeedAdminAsync()
    {
        var admins = await userManager.GetUsersInRoleAsync(Roles.Admin);
        if (admins.Count > 0) return;

        var email = configuration["AdminSeed:Email"];
        var password = configuration["AdminSeed:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("No admin user exists. Set AdminSeed:Email and AdminSeed:Password (environment variables or user-secrets) and restart to create one.");
            return;
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, FullName = "مدیر سایت", LockoutEnabled = true };
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                logger.LogError("Could not create admin user: {Errors}", string.Join("; ", result.Errors.Select(e => e.Description)));
                return;
            }
        }
        await userManager.AddToRoleAsync(user, Roles.Admin);
        logger.LogInformation("Admin user {Email} created.", email);
    }

    private static SiteSettings DefaultSettings() => new()
    {
        SiteName = "آموزشگاه آزما",
        Tagline = "یادگیری مهارت‌های کاربردی، با برنامه و پشتیبانی",
        HeroTitle = "مهارتی بیاموزید که در کار واقعی به کارتان بیاید",
        HeroText = "دوره‌های حضوری و آنلاین با مدرسین باتجربه، پروژه‌محور و همراه با پشتیبانی. ثبت‌نام و پرداخت آنلاین در چند دقیقه.",
        HeroImagePath = "img/samples/hero.webp",
        Phone = "021-12345678",
        SupportMobile = "09120000000",
        Email = "info@example.com",
        Address = "تهران، خیابان نمونه، پلاک ۱۰، طبقه دوم",
        WorkingHours = "شنبه تا چهارشنبه، ۹ تا ۱۸",
        AboutText = "آموزشگاه آزما با هدف آموزش مهارت‌های کاربردی و مورد نیاز بازار کار، دوره‌های تخصصی را با رویکرد پروژه‌محور برگزار می‌کند.",
        FooterText = "تمامی حقوق این وب‌سایت محفوظ است.",
        TermsText = "۱. ثبت‌نام پس از پرداخت موفق و دریافت کد پیگیری قطعی می‌شود.\n۲. در صورت لغو دوره از سوی آموزشگاه، کل مبلغ بازگردانده می‌شود.\n۳. شرایط انصراف هر دوره در صفحه همان دوره درج شده است.\n۴. اطلاعات شخصی شما فقط برای هماهنگی دوره‌ها استفاده می‌شود.",
        PaymentEnabled = true,
        UseSandbox = true,
        PaymentDescription = "پرداخت سفارش {0}",
        SeatReservationMinutes = 30,
        ShowInstructorsOnHome = true,
    };

    private async Task SeedSampleDataAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        var catDev = new CourseCategory { Name = "برنامه‌نویسی", Slug = "programming", SortOrder = 1 };
        var catDesign = new CourseCategory { Name = "طراحی", Slug = "design", SortOrder = 2 };
        var catBiz = new CourseCategory { Name = "کسب‌وکار", Slug = "business", SortOrder = 3 };
        var pBooks = new ProductCategory { Name = "کتاب", Slug = "books", SortOrder = 1 };
        var pDigital = new ProductCategory { Name = "محصولات دیجیتال", Slug = "digital", SortOrder = 2 };

        var i1 = new Instructor { FullName = "مهندس سارا محمدی", Title = "توسعه‌دهنده ارشد دات‌نت", Bio = "بیش از ۱۰ سال سابقه توسعه نرم‌افزارهای سازمانی.", PhotoPath = "img/samples/instructor-1.webp", SortOrder = 1, CreatedAt = now };
        var i2 = new Instructor { FullName = "علی رضایی", Title = "طراح محصول (UI/UX)", Bio = "طراح رابط کاربری با تجربه کار در استارتاپ‌های بزرگ.", PhotoPath = "img/samples/instructor-2.webp", SortOrder = 2, CreatedAt = now };
        var i3 = new Instructor { FullName = "دکتر مریم احمدی", Title = "مشاور مدیریت و بازاریابی", Bio = "مدرس دانشگاه و مشاور کسب‌وکارهای کوچک و متوسط.", PhotoPath = "img/samples/instructor-3.webp", SortOrder = 3, CreatedAt = now };

        var c1 = new Course
        {
            Title = "دوره جامع ASP.NET Core", Slug = "aspnet-core", Category = catDev, Instructor = i1,
            ShortDescription = "از مبانی تا انتشار یک وب‌اپلیکیشن واقعی با ASP.NET Core و Blazor.",
            Description = "در این دوره به صورت پروژه‌محور یک سامانه کامل فروشگاهی را از صفر طراحی و پیاده‌سازی می‌کنیم.\nمباحث شامل معماری لایه‌ای، Entity Framework Core، احراز هویت و انتشار روی سرور است.",
            Syllabus = "مقدمه و نصب ابزارها\nمبانی ASP.NET Core و Middleware\nBlazor و کامپوننت‌ها\nEntity Framework Core و SQL Server\nاحراز هویت با Identity\nاتصال به درگاه پرداخت\nانتشار روی IIS و لینوکس",
            Prerequisites = "آشنایی با زبان C# در حد مقدماتی",
            RegistrationTerms = "در صورت انصراف تا ۴۸ ساعت قبل از شروع دوره، ۸۰٪ مبلغ بازگردانده می‌شود.",
            CoverImagePath = "img/samples/course-1.webp", Duration = "۴۸ ساعت (۱۶ جلسه)",
            StartDate = today.AddDays(14), EndDate = today.AddDays(70), RegistrationEndsOn = today.AddDays(12),
            DeliveryMode = DeliveryMode.Hybrid, Location = "تهران، خیابان نمونه، پلاک ۱۰، کلاس ۲", OnlinePlatform = "اسکای‌روم",
            SupportPhone = "09120000000", Capacity = 20, Price = 6_500_000, DiscountPrice = 5_200_000,
            Status = PublishStatus.Published, IsFeatured = true, SortOrder = 1, CreatedAt = now,
            Schedules =
            [
                new CourseSchedule { DayOfWeek = DayOfWeek.Saturday, StartTime = new TimeOnly(17, 0), EndTime = new TimeOnly(20, 0) },
                new CourseSchedule { DayOfWeek = DayOfWeek.Monday, StartTime = new TimeOnly(17, 0), EndTime = new TimeOnly(20, 0) },
            ],
            RegistrationFields =
            [
                new CourseRegistrationField { Label = "میزان آشنایی با برنامه‌نویسی", FieldType = RegistrationFieldType.Select, Options = "مبتدی\nمتوسط\nپیشرفته", IsRequired = true, SortOrder = 0 },
                new CourseRegistrationField { Label = "شیوه شرکت در کلاس", FieldType = RegistrationFieldType.Select, Options = "حضوری\nآنلاین", IsRequired = true, SortOrder = 1 },
            ],
            Images = [new CourseImage { Path = "img/samples/gallery-1.webp", SortOrder = 0 }, new CourseImage { Path = "img/samples/gallery-2.webp", SortOrder = 1 }],
        };
        var c2 = new Course
        {
            Title = "طراحی رابط کاربری با Figma", Slug = "figma-ui-design", Category = catDesign, Instructor = i2,
            ShortDescription = "اصول طراحی رابط کاربری و ساخت نمونه اولیه تعاملی در فیگما.",
            Description = "یاد می‌گیرید چطور یک محصول دیجیتال را از وایرفریم تا طراحی نهایی و تحویل به برنامه‌نویس آماده کنید.",
            Syllabus = "اصول طراحی بصری\nکار با Auto Layout\nساخت Design System\nنمونه‌سازی تعاملی\nتحویل طراحی به تیم فنی",
            Prerequisites = "نیازی به پیش‌نیاز خاصی نیست.",
            CoverImagePath = "img/samples/course-2.webp", Duration = "۲۴ ساعت",
            StartDate = today.AddDays(7), EndDate = today.AddDays(40),
            DeliveryMode = DeliveryMode.Online, OnlinePlatform = "Google Meet",
            Capacity = 30, Price = 3_800_000, Status = PublishStatus.Published, IsFeatured = true, SortOrder = 2, CreatedAt = now,
            Schedules = [new CourseSchedule { DayOfWeek = DayOfWeek.Wednesday, StartTime = new TimeOnly(18, 0), EndTime = new TimeOnly(21, 0) }],
        };
        var c3 = new Course
        {
            Title = "بازاریابی دیجیتال برای کسب‌وکارهای کوچک", Slug = "digital-marketing", Category = catBiz, Instructor = i3,
            ShortDescription = "تولید محتوا، شبکه‌های اجتماعی و تبلیغات آنلاین با بودجه محدود.",
            Description = "یک برنامه عملی برای جذب مشتری آنلاین، مخصوص صاحبان کسب‌وکارهای کوچک.",
            Syllabus = "شناخت مخاطب\nاستراتژی محتوا\nاینستاگرام و تلگرام\nتبلیغات کلیکی\nسنجش نتایج",
            CoverImagePath = "img/samples/course-3.webp", Duration = "۱۲ ساعت",
            StartDate = today.AddDays(-10), EndDate = today.AddDays(20),
            DeliveryMode = DeliveryMode.InPerson, Location = "تهران، خیابان نمونه، پلاک ۱۰",
            Capacity = 15, Price = 2_400_000, DiscountPrice = 1_900_000, Status = PublishStatus.Published, SortOrder = 3, CreatedAt = now,
            Schedules = [new CourseSchedule { DayOfWeek = DayOfWeek.Thursday, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(13, 0) }],
        };
        var c4 = new Course
        {
            Title = "کارگاه رایگان آشنایی با هوش مصنوعی", Slug = "ai-intro-workshop", Category = catDev, Instructor = i1,
            ShortDescription = "یک جلسه رایگان برای آشنایی با ابزارهای هوش مصنوعی در کار روزمره.",
            Description = "در این کارگاه کاربردهای عملی ابزارهای هوش مصنوعی را بررسی می‌کنیم.",
            CoverImagePath = "img/samples/course-4.webp", Duration = "۲ ساعت",
            StartDate = today.AddDays(5), EndDate = today.AddDays(5),
            DeliveryMode = DeliveryMode.Online, OnlinePlatform = "اسکای‌روم",
            Capacity = 100, Price = 0, Status = PublishStatus.Published, SortOrder = 4, CreatedAt = now,
            Schedules = [new CourseSchedule { DayOfWeek = DayOfWeek.Tuesday, StartTime = new TimeOnly(19, 0), EndTime = new TimeOnly(21, 0) }],
        };

        db.AddRange(c1, c2, c3, c4);
        db.Products.AddRange(
            new Product { Name = "کتاب آموزش C# از مبتدی تا پیشرفته", Slug = "csharp-book", Sku = "BK-CS-01", Category = pBooks, ProductType = ProductType.Physical, ShortDescription = "کتاب چاپی ۴۵۰ صفحه‌ای همراه با تمرین.", Description = "مرجع کامل زبان C# به زبان فارسی، همراه با مثال‌های کاربردی.", ImagePath = "img/samples/product-1.webp", Price = 450_000, DiscountPrice = 390_000, Stock = 25, Status = PublishStatus.Published, IsFeatured = true, SortOrder = 1, CreatedAt = now },
            new Product { Name = "بسته فایل‌های طراحی UI Kit", Slug = "ui-kit", Sku = "DG-UIKIT-01", Category = pDigital, ProductType = ProductType.Digital, ShortDescription = "بیش از ۲۰۰ کامپوننت آماده فیگما.", Description = "فایل لینک دانلود پس از پرداخت از طریق پشتیبانی ارسال می‌شود.", ImagePath = "img/samples/product-2.webp", Price = 900_000, Status = PublishStatus.Published, SortOrder = 2, CreatedAt = now },
            new Product { Name = "دفترچه برنامه‌ریزی یادگیری", Slug = "planner", Sku = "BK-PLN-01", Category = pBooks, ProductType = ProductType.Physical, ShortDescription = "دفترچه ۱۲ هفته‌ای برای برنامه‌ریزی مطالعه.", ImagePath = "img/samples/product-3.webp", Price = 180_000, Stock = 0, Status = PublishStatus.Published, SortOrder = 3, CreatedAt = now });

        db.FaqItems.AddRange(
            new FaqItem { Question = "چطور در دوره‌ها ثبت‌نام کنم؟", Answer = "دوره موردنظر را انتخاب کنید، روی «ثبت‌نام و پرداخت» بزنید، فرم کوتاه را تکمیل و هزینه را به صورت آنلاین پرداخت کنید.", SortOrder = 1 },
            new FaqItem { Question = "پس از پرداخت چه اتفاقی می‌افتد؟", Answer = "بلافاصله کد پیگیری نمایش داده می‌شود و ثبت‌نام شما قطعی است. همکاران ما پیش از شروع دوره با شما تماس می‌گیرند.", SortOrder = 2 },
            new FaqItem { Question = "اگر پرداخت ناموفق بود چه کنم؟", Answer = "اگر مبلغی از حساب شما کسر شده باشد، حداکثر ظرف ۷۲ ساعت توسط بانک بازگردانده می‌شود. می‌توانید از صفحه سفارش دوباره پرداخت کنید.", SortOrder = 3 },
            new FaqItem { Question = "آیا گواهی پایان دوره ارائه می‌شود؟", Answer = "بله، برای شرکت‌کنندگانی که حداقل ۸۰٪ جلسات را حضور داشته باشند گواهی صادر می‌شود.", SortOrder = 4 });

        db.FeatureItems.AddRange(
            new FeatureItem { Title = "مدرسین باتجربه", Description = "آموزش توسط متخصصانی که در پروژه‌های واقعی کار می‌کنند.", Icon = "bi-person-video3", SortOrder = 1 },
            new FeatureItem { Title = "آموزش پروژه‌محور", Description = "هر دوره با یک پروژه عملی و قابل ارائه به پایان می‌رسد.", Icon = "bi-kanban", SortOrder = 2 },
            new FeatureItem { Title = "پشتیبانی پس از دوره", Description = "پاسخ به سوالات شما حتی پس از پایان کلاس‌ها.", Icon = "bi-headset", SortOrder = 3 },
            new FeatureItem { Title = "گواهی معتبر", Description = "دریافت گواهی پایان دوره برای ارائه در رزومه.", Icon = "bi-award", SortOrder = 4 });

        db.Instructors.Add(i3);
        await db.SaveChangesAsync(ct);
    }
}
