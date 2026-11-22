using Azma.Domain.Sales;
using Azma.Domain.Common;

namespace Azma.Domain.Catalog;

public enum DeliveryMode
{
    InPerson = 0,
    Online = 1,
    Hybrid = 2,
}

public enum RegistrationState
{
    Open,
    NotStarted,
    Closed,
    Full,
    NotPublished,
}

public class Course : AuditableEntity
{
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    /// <summary>One syllabus heading per line.</summary>
    public string? Syllabus { get; set; }
    public string? Prerequisites { get; set; }
    public string? RegistrationTerms { get; set; }
    public string? CoverImagePath { get; set; }

    public int? CategoryId { get; set; }
    public CourseCategory? Category { get; set; }
    public int? InstructorId { get; set; }
    public Instructor? Instructor { get; set; }

    public string? Duration { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public DateOnly? RegistrationStartsOn { get; set; }
    public DateOnly? RegistrationEndsOn { get; set; }
    public DeliveryMode DeliveryMode { get; set; }
    public string? Location { get; set; }
    public string? OnlinePlatform { get; set; }
    public string? SupportPhone { get; set; }

    /// <summary>Null means unlimited capacity.</summary>
    public int? Capacity { get; set; }

    /// <summary>Prices in Toman.</summary>
    public long Price { get; set; }
    public long? DiscountPrice { get; set; }

    public PublishStatus Status { get; set; } = PublishStatus.Draft;
    public bool IsFeatured { get; set; }
    public bool AllowDuplicateEnrollment { get; set; }
    public int SortOrder { get; set; }

    public List<CourseImage> Images { get; set; } = [];
    public List<CourseSchedule> Schedules { get; set; } = [];
    public List<CourseRegistrationField> RegistrationFields { get; set; } = [];
    public List<Enrollment> Enrollments { get; set; } = [];

    public long FinalPrice => Money.FinalPrice(Price, DiscountPrice);

    public IEnumerable<string> SyllabusItems => SplitLines(Syllabus);

    /// <param name="today">Today's date in the site's local time zone (Asia/Tehran).</param>
    public RegistrationState GetRegistrationState(DateOnly today, int usedSeats)
    {
        if (Status != PublishStatus.Published) return RegistrationState.NotPublished;
        if (RegistrationStartsOn is { } start && today < start) return RegistrationState.NotStarted;
        if (RegistrationEndsOn is { } end && today > end) return RegistrationState.Closed;
        if (Capacity is { } cap && usedSeats >= cap) return RegistrationState.Full;
        return RegistrationState.Open;
    }

    public int? RemainingSeats(int usedSeats) => Capacity is { } cap ? Math.Max(0, cap - usedSeats) : null;

    internal static IEnumerable<string> SplitLines(string? text)
        => string.IsNullOrWhiteSpace(text)
            ? []
            : text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

public class CourseImage : Entity
{
    public int CourseId { get; set; }
    public Course? Course { get; set; }
    public string Path { get; set; } = "";
    public string? Caption { get; set; }
    public int SortOrder { get; set; }
}

public class CourseSchedule : Entity
{
    public int CourseId { get; set; }
    public Course? Course { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string? Note { get; set; }
}

public enum RegistrationFieldType
{
    Text = 0,
    TextArea = 1,
    Number = 2,
    Select = 3,
    Checkbox = 4,
}

/// <summary>Extra admin-defined field on a course's registration form.</summary>
public class CourseRegistrationField : Entity
{
    public int CourseId { get; set; }
    public Course? Course { get; set; }
    public string Label { get; set; } = "";
    public RegistrationFieldType FieldType { get; set; }
    /// <summary>Options for Select fields, one per line.</summary>
    public string? Options { get; set; }
    public bool IsRequired { get; set; }
    public int SortOrder { get; set; }

    public IEnumerable<string> OptionItems => Course.SplitLines(Options);
}
