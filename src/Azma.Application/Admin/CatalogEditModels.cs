using System.ComponentModel.DataAnnotations;
using Azma.Domain.Catalog;
using Azma.Domain.Common;

namespace Azma.Application.Admin;

public sealed class CourseEditModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "عنوان دوره الزامی است.")]
    [StringLength(200, ErrorMessage = "عنوان حداکثر ۲۰۰ حرف است.")]
    public string Title { get; set; } = "";

    [StringLength(150)]
    public string? Slug { get; set; }

    public int? CategoryId { get; set; }
    public int? InstructorId { get; set; }

    [StringLength(500)] public string? ShortDescription { get; set; }
    [StringLength(20000)] public string? Description { get; set; }
    [StringLength(10000)] public string? Syllabus { get; set; }
    [StringLength(4000)] public string? Prerequisites { get; set; }
    [StringLength(4000)] public string? RegistrationTerms { get; set; }
    public string? CoverImagePath { get; set; }
    [StringLength(100)] public string? Duration { get; set; }

    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public DateOnly? RegistrationStartsOn { get; set; }
    public DateOnly? RegistrationEndsOn { get; set; }

    public DeliveryMode DeliveryMode { get; set; }
    [StringLength(500)] public string? Location { get; set; }
    [StringLength(200)] public string? OnlinePlatform { get; set; }
    [StringLength(30)] public string? SupportPhone { get; set; }

    [Range(1, 100000, ErrorMessage = "ظرفیت باید عددی مثبت باشد (برای ظرفیت نامحدود خالی بگذارید).")]
    public int? Capacity { get; set; }

    [Range(0, 100_000_000_000, ErrorMessage = "قیمت نامعتبر است.")]
    public long Price { get; set; }

    [Range(0, 100_000_000_000, ErrorMessage = "قیمت با تخفیف نامعتبر است.")]
    public long? DiscountPrice { get; set; }

    public PublishStatus Status { get; set; } = PublishStatus.Draft;
    public bool IsFeatured { get; set; }
    public bool AllowDuplicateEnrollment { get; set; }
    public int SortOrder { get; set; }

    public List<ScheduleEditModel> Schedules { get; set; } = [];
    public List<FieldEditModel> Fields { get; set; } = [];
    public List<ImageEditModel> Images { get; set; } = [];
}

public sealed class ScheduleEditModel
{
    public int Id { get; set; }
    public DayOfWeek DayOfWeek { get; set; } = DayOfWeek.Saturday;
    public TimeOnly StartTime { get; set; } = new(17, 0);
    public TimeOnly EndTime { get; set; } = new(19, 0);
    [StringLength(200)] public string? Note { get; set; }
}

public sealed class FieldEditModel
{
    public int Id { get; set; }
    [Required(ErrorMessage = "عنوان فیلد الزامی است.")]
    [StringLength(150)]
    public string Label { get; set; } = "";
    public RegistrationFieldType FieldType { get; set; }
    [StringLength(2000)] public string? Options { get; set; }
    public bool IsRequired { get; set; }
}

public sealed class ImageEditModel
{
    public int Id { get; set; }
    public string Path { get; set; } = "";
    [StringLength(200)] public string? Caption { get; set; }
}

public sealed class ProductEditModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "نام محصول الزامی است.")]
    [StringLength(200)]
    public string Name { get; set; } = "";

    [StringLength(150)] public string? Slug { get; set; }

    [Required(ErrorMessage = "کد محصول الزامی است.")]
    [StringLength(50)]
    [RegularExpression(@"^[A-Za-z0-9\-_]+$", ErrorMessage = "کد محصول فقط شامل حروف انگلیسی، عدد، خط تیره و زیرخط باشد.")]
    public string Sku { get; set; } = "";

    [StringLength(500)] public string? ShortDescription { get; set; }
    [StringLength(20000)] public string? Description { get; set; }
    public string? ImagePath { get; set; }
    public ProductType ProductType { get; set; }
    public int? CategoryId { get; set; }

    [Range(0, 100_000_000_000, ErrorMessage = "قیمت نامعتبر است.")]
    public long Price { get; set; }
    [Range(0, 100_000_000_000, ErrorMessage = "قیمت با تخفیف نامعتبر است.")]
    public long? DiscountPrice { get; set; }

    [Range(0, 1_000_000, ErrorMessage = "موجودی نامعتبر است.")]
    public int? Stock { get; set; }
    public bool IsAvailable { get; set; } = true;
    public PublishStatus Status { get; set; } = PublishStatus.Draft;
    public bool IsFeatured { get; set; }
    public int SortOrder { get; set; }

    public List<ImageEditModel> Images { get; set; } = [];
}
