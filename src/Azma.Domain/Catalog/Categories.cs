using Azma.Domain.Common;

namespace Azma.Domain.Catalog;

public class CourseCategory : Entity
{
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public int SortOrder { get; set; }
    public List<Course> Courses { get; set; } = [];
}

public class ProductCategory : Entity
{
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public int SortOrder { get; set; }
    public List<Product> Products { get; set; } = [];
}
