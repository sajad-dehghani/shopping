using Azma.Domain.Sales;
using Azma.Domain.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Azma.Infrastructure.Persistence.Configurations;

public sealed class PaymentReceiptConfiguration : IEntityTypeConfiguration<PaymentReceipt>
{
    public void Configure(EntityTypeBuilder<PaymentReceipt> b)
    {
        b.Property(x => x.PayerName).HasMaxLength(100).IsRequired();
        b.Property(x => x.PayerMobile).HasMaxLength(11).IsRequired().IsUnicode(false);
        b.Property(x => x.CardLast4).HasMaxLength(4).IsUnicode(false);
        b.Property(x => x.BankReference).HasMaxLength(50);
        b.Property(x => x.CustomerNote).HasMaxLength(500);
        b.Property(x => x.ImagePath).HasMaxLength(300).IsRequired();
        b.Property(x => x.ReviewedBy).HasMaxLength(256);
        b.Property(x => x.RejectReason).HasMaxLength(500);
        b.HasIndex(x => new { x.Status, x.CreatedAt });
        b.ToTable(t => t.HasCheckConstraint("CK_PaymentReceipts_Amount", "[Amount] > 0"));
    }
}

public sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> b)
    {
        b.Property(x => x.TicketNumber).HasMaxLength(20).IsRequired().IsUnicode(false);
        b.HasIndex(x => x.TicketNumber).IsUnique();
        b.HasIndex(x => x.PublicId).IsUnique();
        b.HasIndex(x => new { x.Status, x.LastActivityAt });
        b.HasIndex(x => x.Mobile);
        b.Property(x => x.FullName).HasMaxLength(100).IsRequired();
        b.Property(x => x.Mobile).HasMaxLength(11).IsRequired().IsUnicode(false);
        b.Property(x => x.Subject).HasMaxLength(150).IsRequired();
        b.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.SetNull);
        b.HasMany(x => x.Messages).WithOne(m => m.Ticket).HasForeignKey(m => m.TicketId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class TicketMessageConfiguration : IEntityTypeConfiguration<TicketMessage>
{
    public void Configure(EntityTypeBuilder<TicketMessage> b)
    {
        b.Property(x => x.Body).HasMaxLength(4000).IsRequired();
        b.Property(x => x.AuthorName).HasMaxLength(256);
    }
}
