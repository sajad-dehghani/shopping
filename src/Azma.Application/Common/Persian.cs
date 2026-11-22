using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Azma.Application.Common;

/// <summary>Persian (Jalali) dates, Persian digits, money formatting and input normalization.</summary>
public static partial class Persian
{
    private static readonly PersianCalendar Calendar = new();

    private static readonly string[] MonthNames =
        ["فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور", "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند"];

    public static string DayName(DayOfWeek day) => day switch
    {
        DayOfWeek.Saturday => "شنبه",
        DayOfWeek.Sunday => "یکشنبه",
        DayOfWeek.Monday => "دوشنبه",
        DayOfWeek.Tuesday => "سه‌شنبه",
        DayOfWeek.Wednesday => "چهارشنبه",
        DayOfWeek.Thursday => "پنجشنبه",
        _ => "جمعه",
    };

    /// <summary>Days ordered as in the Iranian week (Saturday first).</summary>
    public static readonly DayOfWeek[] WeekDays =
        [DayOfWeek.Saturday, DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday];

    /// <summary>"1405/09/01" style (Latin digits) — used for editable inputs.</summary>
    public static string ToJalaliInput(DateOnly date)
    {
        var dt = date.ToDateTime(TimeOnly.MinValue);
        return $"{Calendar.GetYear(dt):0000}/{Calendar.GetMonth(dt):00}/{Calendar.GetDayOfMonth(dt):00}";
    }

    public static string ToJalali(DateOnly? date) => date is { } d ? Digits(ToJalaliInput(d)) : "—";

    public static string ToJalali(DateTime? dateTime, bool withTime = false)
    {
        if (dateTime is not { } dt) return "—";
        var s = $"{Calendar.GetYear(dt):0000}/{Calendar.GetMonth(dt):00}/{Calendar.GetDayOfMonth(dt):00}";
        if (withTime) s += $" - {dt:HH:mm}";
        return Digits(s);
    }

    /// <summary>"۱ آذر ۱۴۰۵"</summary>
    public static string ToJalaliLong(DateOnly? date)
    {
        if (date is not { } d) return "—";
        var dt = d.ToDateTime(TimeOnly.MinValue);
        return Digits($"{Calendar.GetDayOfMonth(dt)} {MonthNames[Calendar.GetMonth(dt) - 1]} {Calendar.GetYear(dt)}");
    }

    public static bool TryParseJalali(string? input, out DateOnly date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(input)) return false;
        var parts = NormalizeDigits(input).Trim().Split('/', '-', '.');
        if (parts.Length != 3
            || !int.TryParse(parts[0], out var y) || !int.TryParse(parts[1], out var m) || !int.TryParse(parts[2], out var d))
            return false;
        try
        {
            if (y < 1300 || y > 1500 || m < 1 || m > 12 || d < 1 || d > Calendar.GetDaysInMonth(y, m)) return false;
            date = DateOnly.FromDateTime(Calendar.ToDateTime(y, m, d, 0, 0, 0, 0));
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    public static string Digits(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        var sb = new StringBuilder(text.Length);
        foreach (var c in text) sb.Append(c is >= '0' and <= '9' ? (char)('۰' + (c - '0')) : c);
        return sb.ToString();
    }

    public static string Digits(long number) => Digits(number.ToString("N0", CultureInfo.InvariantCulture).Replace(",", "٬"));
    public static string Digits(int number) => Digits((long)number);

    public static string Toman(long amount) => amount == 0 ? "رایگان" : $"{Digits(amount)} تومان";

    /// <summary>Converts Persian/Arabic digits to Latin digits.</summary>
    public static string NormalizeDigits(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            sb.Append(c switch
            {
                >= '۰' and <= '۹' => (char)('0' + (c - '۰')),
                >= '٠' and <= '٩' => (char)('0' + (c - '٠')),
                _ => c,
            });
        }
        return sb.ToString();
    }

    /// <summary>Normalizes Arabic Yeh/Kaf to Persian and trims whitespace.</summary>
    public static string NormalizeText(string? text)
        => (text ?? "").Replace('ي', 'ی').Replace('ك', 'ک').Trim();

    /// <summary>Normalizes to 09xxxxxxxxx; returns null when invalid.</summary>
    public static string? NormalizeMobile(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var s = NonDigits().Replace(NormalizeDigits(input), "");
        if (s.StartsWith("0098")) s = "0" + s[4..];
        else if (s.StartsWith("98") && s.Length == 12) s = "0" + s[2..];
        else if (s.Length == 10 && s.StartsWith('9')) s = "0" + s;
        return MobileRegex().IsMatch(s) ? s : null;
    }

    [GeneratedRegex(@"\D")]
    private static partial Regex NonDigits();

    [GeneratedRegex(@"^09\d{9}$")]
    private static partial Regex MobileRegex();

    /// <summary>Builds a URL slug, keeping Persian letters.</summary>
    public static string Slugify(string text)
    {
        var s = NormalizeText(NormalizeDigits(text)).ToLowerInvariant();
        s = SlugInvalid().Replace(s, "-");
        s = MultiDash().Replace(s, "-").Trim('-');
        return s.Length > 120 ? s[..120].Trim('-') : s;
    }

    [GeneratedRegex(@"[^\p{L}\p{Nd}]+")]
    private static partial Regex SlugInvalid();

    [GeneratedRegex(@"-{2,}")]
    private static partial Regex MultiDash();
}
