using System.ComponentModel.DataAnnotations;
using Azma.Application.Abstractions;
using Azma.Domain.Content;
using Microsoft.EntityFrameworkCore;

namespace Azma.Application.Common;

public sealed class ContactInput
{
    [Required(ErrorMessage = "نام را وارد کنید.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "نام نامعتبر است.")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "شماره تماس را وارد کنید.")]
    [StringLength(20)]
    public string Mobile { get; set; } = "";

    [OptionalEmail]
    [StringLength(150)]
    public string? Email { get; set; }

    [Required(ErrorMessage = "متن پیام را وارد کنید.")]
    [StringLength(2000, MinimumLength = 5, ErrorMessage = "متن پیام باید بین ۵ تا ۲۰۰۰ حرف باشد.")]
    public string Message { get; set; } = "";

    /// <summary>Honeypot field: real users never fill it.</summary>
    public string? Website { get; set; }
}

public sealed class ContactService(IAppDbContextFactory dbFactory, ISiteClock clock)
{
    public async Task<Result> SubmitAsync(ContactInput input, string? ip, CancellationToken ct = default)
    {
        if (!string.IsNullOrEmpty(input.Website)) return Result.Ok(); // silently drop bots
        var mobile = Persian.NormalizeMobile(input.Mobile);
        if (mobile is null) return Result.Fail("شماره موبایل معتبر نیست.");

        await using var db = await dbFactory.CreateAsync(ct);
        var since = clock.UtcNow.AddHours(-1);
        if (ip is not null && await db.ContactMessages.CountAsync(m => m.IpAddress == ip && m.CreatedAt > since, ct) >= 5)
            return Result.Fail("تعداد پیام‌های ارسالی زیاد است. لطفاً بعداً تلاش کنید.");

        db.ContactMessages.Add(new ContactMessage
        {
            FullName = Persian.NormalizeText(input.FullName),
            Mobile = mobile,
            Email = string.IsNullOrWhiteSpace(input.Email) ? null : input.Email.Trim(),
            Message = Persian.NormalizeText(input.Message),
            IpAddress = ip,
            CreatedAt = clock.UtcNow,
        });
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
