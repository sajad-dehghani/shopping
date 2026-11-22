using Azma.Domain.Catalog;
using Azma.Domain.Content;

namespace Azma.Application.Catalog;

public enum CourseTimeFilter
{
    All = 0,
    OpenForRegistration = 1,
    Running = 2,
    Upcoming = 3,
}

public sealed record CourseFilter(string? Query = null, string? Category = null, CourseTimeFilter When = CourseTimeFilter.All, int Page = 1, int PageSize = 12);

public sealed record ProductFilter(string? Query = null, string? Category = null, int Page = 1, int PageSize = 12);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => PageSize <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
}

public sealed record CategoryOption(int Id, string Name, string Slug);

public sealed class CourseCard
{
    public int Id { get; init; }
    public string Title { get; init; } = "";
    public string Slug { get; init; } = "";
    public string? ShortDescription { get; init; }
    public string? CoverImagePath { get; init; }
    public string? InstructorName { get; init; }
    public string? CategoryName { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public DeliveryMode DeliveryMode { get; init; }
    public long Price { get; init; }
    public long? DiscountPrice { get; init; }
    public long FinalPrice => Domain.Common.Money.FinalPrice(Price, DiscountPrice);
    public int? Capacity { get; init; }
    public bool IsFeatured { get; init; }
    public Domain.Common.PublishStatus Status { get; init; }
    public DateOnly? RegistrationStartsOn { get; init; }
    public DateOnly? RegistrationEndsOn { get; init; }
    public int UsedSeats { get; init; }
    public int? RemainingSeats { get; set; }
    public RegistrationState State { get; set; }
}

public sealed class ProductCard
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string Slug { get; init; } = "";
    public string Sku { get; init; } = "";
    public string? ShortDescription { get; init; }
    public string? ImagePath { get; init; }
    public string? CategoryName { get; init; }
    public ProductType ProductType { get; init; }
    public long Price { get; init; }
    public long? DiscountPrice { get; init; }
    public long FinalPrice => Domain.Common.Money.FinalPrice(Price, DiscountPrice);
    public bool InStock { get; init; }
}

public sealed class CourseDetails
{
    public required Course Course { get; init; }
    public int UsedSeats { get; init; }
    public int? RemainingSeats { get; init; }
    public RegistrationState State { get; init; }
    public bool CanRegister => State == RegistrationState.Open;
}

public sealed class HomePageData
{
    public required SiteSettings Settings { get; init; }
    public IReadOnlyList<CourseCard> Courses { get; init; } = [];
    public IReadOnlyList<ProductCard> Products { get; init; } = [];
    public IReadOnlyList<Instructor> Instructors { get; init; } = [];
    public IReadOnlyList<FeatureItem> Features { get; init; } = [];
    public IReadOnlyList<FaqItem> Faqs { get; init; } = [];
}
