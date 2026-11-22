using System.ComponentModel.DataAnnotations;

namespace Azma.Application.Common;

/// <summary>Like [EmailAddress], but treats empty strings (as posted by empty form fields) as "not provided".</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class OptionalEmailAttribute : ValidationAttribute
{
    private static readonly EmailAddressAttribute Inner = new();

    public OptionalEmailAttribute() : base("آدرس ایمیل معتبر نیست.") { }

    public override bool IsValid(object? value)
        => value is null || (value is string s && (string.IsNullOrWhiteSpace(s) || Inner.IsValid(s.Trim())));
}
