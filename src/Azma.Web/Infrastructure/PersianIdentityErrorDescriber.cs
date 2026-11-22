using Microsoft.AspNetCore.Identity;

namespace Azma.Web.Infrastructure;

public sealed class PersianIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError PasswordTooShort(int length) => new() { Code = nameof(PasswordTooShort), Description = $"رمز عبور باید حداقل {length} کاراکتر باشد." };
    public override IdentityError PasswordRequiresDigit() => new() { Code = nameof(PasswordRequiresDigit), Description = "رمز عبور باید حداقل یک رقم داشته باشد." };
    public override IdentityError PasswordRequiresLower() => new() { Code = nameof(PasswordRequiresLower), Description = "رمز عبور باید حداقل یک حرف کوچک انگلیسی داشته باشد." };
    public override IdentityError PasswordRequiresUpper() => new() { Code = nameof(PasswordRequiresUpper), Description = "رمز عبور باید حداقل یک حرف بزرگ انگلیسی داشته باشد." };
    public override IdentityError PasswordRequiresNonAlphanumeric() => new() { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "رمز عبور باید حداقل یک نماد داشته باشد." };
    public override IdentityError PasswordMismatch() => new() { Code = nameof(PasswordMismatch), Description = "رمز عبور فعلی نادرست است." };
    public override IdentityError DuplicateEmail(string email) => new() { Code = nameof(DuplicateEmail), Description = "این ایمیل قبلاً ثبت شده است." };
    public override IdentityError DuplicateUserName(string userName) => new() { Code = nameof(DuplicateUserName), Description = "این نام کاربری قبلاً ثبت شده است." };
    public override IdentityError InvalidEmail(string? email) => new() { Code = nameof(InvalidEmail), Description = "ایمیل نامعتبر است." };
    public override IdentityError InvalidUserName(string? userName) => new() { Code = nameof(InvalidUserName), Description = "نام کاربری نامعتبر است." };
}
