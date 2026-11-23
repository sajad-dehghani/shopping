using Azma.Domain.Auditing;
using Azma.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Azma.Infrastructure.Persistence.Configurations;

public sealed class SiteSettingsConfiguration : IEntityTypeConfiguration<SiteSettings>
{
    public void Configure(EntityTypeBuilder<SiteSettings> b)
    {
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.SiteName).HasMaxLength(100).IsRequired();
        b.Property(x => x.Tagline).HasMaxLength(200);
        b.Property(x => x.HeroTitle).HasMaxLength(200);
        b.Property(x => x.HeroText).HasMaxLength(1000);
        b.Property(x => x.LogoPath).HasMaxLength(300);
        b.Property(x => x.HeroImagePath).HasMaxLength(300);
        b.Property(x => x.Phone).HasMaxLength(30);
        b.Property(x => x.SupportMobile).HasMaxLength(30);
        b.Property(x => x.Email).HasMaxLength(150);
        b.Property(x => x.Address).HasMaxLength(500);
        b.Property(x => x.WorkingHours).HasMaxLength(200);
        b.Property(x => x.AboutText).HasMaxLength(4000);
        b.Property(x => x.FooterText).HasMaxLength(1000);
        b.Property(x => x.TermsText).HasMaxLength(10000);
        b.Property(x => x.Instagram).HasMaxLength(300);
        b.Property(x => x.Telegram).HasMaxLength(300);
        b.Property(x => x.WhatsApp).HasMaxLength(300);
        b.Property(x => x.LinkedIn).HasMaxLength(300);
        b.Property(x => x.Aparat).HasMaxLength(300);
        b.Property(x => x.EnamadHtmlCode).HasMaxLength(2000);
        b.Property(x => x.PaymentDescription).HasMaxLength(200).IsRequired();
        b.Property(x => x.CardNumber).HasMaxLength(30).IsUnicode(false);
        b.Property(x => x.CardHolderName).HasMaxLength(100);
        b.Property(x => x.BankName).HasMaxLength(100);
        b.Property(x => x.ShebaNumber).HasMaxLength(40).IsUnicode(false);
        b.ToTable(t => t.HasCheckConstraint("CK_SiteSettings_Singleton", "[Id] = 1"));
    }
}

public sealed class FaqItemConfiguration : IEntityTypeConfiguration<FaqItem>
{
    public void Configure(EntityTypeBuilder<FaqItem> b)
    {
        b.Property(x => x.Question).HasMaxLength(300).IsRequired();
        b.Property(x => x.Answer).HasMaxLength(3000).IsRequired();
    }
}

public sealed class FeatureItemConfiguration : IEntityTypeConfiguration<FeatureItem>
{
    public void Configure(EntityTypeBuilder<FeatureItem> b)
    {
        b.Property(x => x.Title).HasMaxLength(150).IsRequired();
        b.Property(x => x.Description).HasMaxLength(500);
        b.Property(x => x.Icon).HasMaxLength(50).IsRequired();
    }
}

public sealed class ContactMessageConfiguration : IEntityTypeConfiguration<ContactMessage>
{
    public void Configure(EntityTypeBuilder<ContactMessage> b)
    {
        b.Property(x => x.FullName).HasMaxLength(100).IsRequired();
        b.Property(x => x.Mobile).HasMaxLength(11).IsRequired().IsUnicode(false);
        b.Property(x => x.Email).HasMaxLength(150);
        b.Property(x => x.Message).HasMaxLength(2000).IsRequired();
        b.Property(x => x.IpAddress).HasMaxLength(45).IsUnicode(false);
        b.HasIndex(x => new { x.IsRead, x.CreatedAt });
        b.HasIndex(x => new { x.IpAddress, x.CreatedAt });
    }
}

public sealed class AdminAuditLogConfiguration : IEntityTypeConfiguration<AdminAuditLog>
{
    public void Configure(EntityTypeBuilder<AdminAuditLog> b)
    {
        b.Property(x => x.UserId).HasMaxLength(450);
        b.Property(x => x.UserName).HasMaxLength(256);
        b.Property(x => x.Action).HasMaxLength(100).IsRequired();
        b.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
        b.Property(x => x.EntityId).HasMaxLength(100);
        b.Property(x => x.Details).HasMaxLength(4000);
        b.Property(x => x.IpAddress).HasMaxLength(45).IsUnicode(false);
        b.HasIndex(x => x.CreatedAt);
        b.HasIndex(x => new { x.EntityType, x.EntityId });
    }
}
