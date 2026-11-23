using Azma.Domain.Common;

namespace Azma.Domain.Content;

public class FaqItem : Entity
{
    public string Question { get; set; } = "";
    public string Answer { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>"Why join our courses" items on the home page.</summary>
public class FeatureItem : Entity
{
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    /// <summary>Bootstrap Icons class name, e.g. "bi-award".</summary>
    public string Icon { get; set; } = "bi-check2-circle";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>A slide in the home page hero carousel.</summary>
public class HeroSlide : Entity
{
    public string? Title { get; set; }
    public string? Subtitle { get; set; }
    public string ImagePath { get; set; } = "";
    public string? ButtonText { get; set; }
    /// <summary>Local path ("/courses") or absolute https URL.</summary>
    public string? ButtonUrl { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ContactMessage : Entity
{
    public string FullName { get; set; } = "";
    public string Mobile { get; set; } = "";
    public string? Email { get; set; }
    public string Message { get; set; } = "";
    public bool IsRead { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}
