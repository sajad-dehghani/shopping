using Azma.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace Azma.Web.Infrastructure;

/// <summary>
/// Builds a T-SQL script that creates (or resets) an admin account, for hosts where the app cannot be run from a
/// command line. Usage, on any PC, no database needed: <c>dotnet Azma.Web.dll admin-sql you@site.ir "Password1"</c>.
/// The password is hashed exactly like ASP.NET Identity does at runtime; the plain password never appears in the script.
/// </summary>
public static class AdminSqlScript
{
    public static string? Validate(string email, string password)
    {
        if (!email.Contains('@') || email.Length > 256 || email.Contains('\'')) return "ایمیل معتبر نیست.";
        if (password.Length < 8 || !password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit))
            return "رمز باید حداقل ۸ کاراکتر و شامل حرف بزرگ و کوچک انگلیسی و عدد باشد.";
        return null;
    }

    public static string Build(string email, string password)
    {
        email = email.Trim();
        var hash = new PasswordHasher<ApplicationUser>().HashPassword(new ApplicationUser(), password);
        return $"""
            -- Create or reset the admin account of the shop. Run once on the site's database (SSMS or the host's SQL panel).
            -- If the account already exists, its password is reset and any lockout is cleared.
            SET NOCOUNT ON;
            SET QUOTED_IDENTIFIER ON;  -- required by the filtered unique indexes on AspNetUsers / AspNetRoles
            SET ANSI_NULLS ON;
            SET XACT_ABORT ON;         -- all or nothing
            DECLARE @Email nvarchar(256) = N'{email}';
            DECLARE @Hash nvarchar(max) = N'{hash}';
            DECLARE @Now datetime2 = SYSUTCDATETIME();

            BEGIN TRAN;

            DECLARE @RoleId nvarchar(450) = (SELECT TOP 1 Id FROM AspNetRoles WHERE NormalizedName = N'ADMIN');
            IF @RoleId IS NULL
            BEGIN
                SET @RoleId = LOWER(CONVERT(nvarchar(36), NEWID()));
                INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
                VALUES (@RoleId, N'Admin', N'ADMIN', LOWER(CONVERT(nvarchar(36), NEWID())));
            END

            DECLARE @UserId nvarchar(450) = (SELECT TOP 1 Id FROM AspNetUsers WHERE NormalizedEmail = UPPER(@Email) OR NormalizedUserName = UPPER(@Email));
            IF @UserId IS NULL
            BEGIN
                SET @UserId = LOWER(CONVERT(nvarchar(36), NEWID()));
                INSERT INTO AspNetUsers (Id, FullName, CreatedAt, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
                                         PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, TwoFactorEnabled,
                                         LockoutEnabled, AccessFailedCount)
                VALUES (@UserId, N'مدیر سایت', @Now, @Email, UPPER(@Email), @Email, UPPER(@Email), 1,
                        @Hash, REPLACE(CONVERT(nvarchar(36), NEWID()), '-', ''), LOWER(CONVERT(nvarchar(36), NEWID())), 0, 0,
                        1, 0);
            END
            ELSE
            BEGIN
                UPDATE AspNetUsers
                SET PasswordHash = @Hash, SecurityStamp = REPLACE(CONVERT(nvarchar(36), NEWID()), '-', ''),
                    LockoutEnd = NULL, AccessFailedCount = 0
                WHERE Id = @UserId;
            END

            IF NOT EXISTS (SELECT 1 FROM AspNetUserRoles WHERE UserId = @UserId AND RoleId = @RoleId)
                INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES (@UserId, @RoleId);

            COMMIT;
            SELECT @Email AS AdminEmail, N'OK' AS Result;
            """;
    }
}
