using Microsoft.AspNetCore.Identity;

namespace Azma.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    [PersonalData]
    public string? FullName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public static class Roles
{
    public const string Admin = "Admin";
}
