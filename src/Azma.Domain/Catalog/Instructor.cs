using Azma.Domain.Common;

namespace Azma.Domain.Catalog;

public class Instructor : AuditableEntity
{
    public string FullName { get; set; } = "";
    public string? Title { get; set; }
    public string? Bio { get; set; }
    public string? PhotoPath { get; set; }
    public bool IsActive { get; set; } = true;
    public bool ShowOnHomePage { get; set; } = true;
    public int SortOrder { get; set; }

    public List<Course> Courses { get; set; } = [];
}
