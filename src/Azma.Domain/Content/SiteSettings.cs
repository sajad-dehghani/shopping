namespace Azma.Domain.Content;

/// <summary>Single-row table (Id = 1) holding admin-editable site settings.</summary>
public class SiteSettings
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;
    public string SiteName { get; set; } = "آزما";
    public string? Tagline { get; set; }
    public string? HeroTitle { get; set; }
    public string? HeroText { get; set; }
    public string? LogoPath { get; set; }
    public string? HeroImagePath { get; set; }
    public string? Phone { get; set; }
    public string? SupportMobile { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? WorkingHours { get; set; }
    public string? AboutText { get; set; }
    public string? FooterText { get; set; }
    public string? TermsText { get; set; }
    public string? Instagram { get; set; }
    public string? Telegram { get; set; }
    public string? WhatsApp { get; set; }
    public string? LinkedIn { get; set; }
    public string? Aparat { get; set; }
    public string? EnamadHtmlCode { get; set; }

    // Payment / general settings (the merchant ID itself lives in secure configuration)
    public bool PaymentEnabled { get; set; } = true;
    public bool UseSandbox { get; set; } = true;
    public string PaymentDescription { get; set; } = "پرداخت سفارش {0}";
    /// <summary>Minutes a pending (unpaid) enrollment holds a seat.</summary>
    public int SeatReservationMinutes { get; set; } = 30;
    public bool ShowInstructorsOnHome { get; set; } = true;

    // Sections: a disabled section disappears from menus, the home page and its own pages.
    public bool CoursesEnabled { get; set; } = true;
    public bool ShopEnabled { get; set; } = true;
    /// <summary>Seconds each hero slide stays on screen.</summary>
    public int SliderIntervalSeconds { get; set; } = 6;

    // Card-to-card (bank transfer) payment
    public bool CardToCardEnabled { get; set; } = true;
    public string? CardNumber { get; set; }
    public string? CardHolderName { get; set; }
    public string? BankName { get; set; }
    public string? ShebaNumber { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
