using System.Diagnostics.CodeAnalysis;
using Azma.Application.Common;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Azma.Web.Components.Admin.Shared;

/// <summary>Text input for Jalali dates (e.g. 1405/09/01) bound to a DateOnly? value.</summary>
public sealed class JalaliDateInput : InputBase<DateOnly?>
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "input");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "type", "text");
        builder.AddAttribute(3, "class", "form-control ltr " + CssClass);
        builder.AddAttribute(4, "placeholder", "1405/01/01");
        builder.AddAttribute(5, "inputmode", "numeric");
        builder.AddAttribute(6, "maxlength", 10);
        builder.AddAttribute(7, "value", CurrentValueAsString);
        builder.AddAttribute(8, "onchange", EventCallback.Factory.CreateBinder<string?>(this, v => CurrentValueAsString = v, CurrentValueAsString));
        builder.CloseElement();
    }

    protected override string? FormatValueAsString(DateOnly? value) => value is { } d ? Persian.ToJalaliInput(d) : "";

    protected override bool TryParseValueFromString(string? value, out DateOnly? result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = null;
            validationErrorMessage = null;
            return true;
        }
        if (Persian.TryParseJalali(value, out var d))
        {
            result = d;
            validationErrorMessage = null;
            return true;
        }
        result = null;
        validationErrorMessage = "تاریخ را به شکل ۱۴۰۵/۰۱/۱۵ وارد کنید.";
        return false;
    }
}
