using Azma.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Azma.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        b.Property(x => x.OrderNumber).HasMaxLength(20).IsRequired();
        b.HasIndex(x => x.OrderNumber).IsUnique();
        b.HasIndex(x => x.PublicId).IsUnique();
        b.HasIndex(x => x.Mobile);
        b.HasIndex(x => new { x.Status, x.CreatedAt });
        b.HasIndex(x => x.TrackingCode);
        b.Property(x => x.CustomerName).HasMaxLength(100).IsRequired();
        b.Property(x => x.Mobile).HasMaxLength(11).IsRequired().IsUnicode(false);
        b.Property(x => x.Email).HasMaxLength(150);
        b.Property(x => x.CustomerNote).HasMaxLength(1000);
        b.Property(x => x.ShippingAddress).HasMaxLength(500);
        b.Property(x => x.PostalCode).HasMaxLength(20);
        b.Property(x => x.TrackingCode).HasMaxLength(50);
        b.Property(x => x.AdminNote).HasMaxLength(4000);
        b.Ignore(x => x.CanRetryPayment);

        b.HasMany(x => x.Items).WithOne(i => i.Order).HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Enrollments).WithOne(e => e.Order).HasForeignKey(e => e.OrderId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Transactions).WithOne(t => t.Order).HasForeignKey(t => t.OrderId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.StatusHistory).WithOne(h => h.Order).HasForeignKey(h => h.OrderId).OnDelete(DeleteBehavior.Cascade);
        b.ToTable(t => t.HasCheckConstraint("CK_Orders_TotalAmount", "[TotalAmount] >= 0"));
    }
}

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> b)
    {
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.HasOne(x => x.Course).WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        b.ToTable(t =>
        {
            t.HasCheckConstraint("CK_OrderItems_Quantity", "[Quantity] > 0");
            t.HasCheckConstraint("CK_OrderItems_Amounts", "[UnitPrice] >= 0 AND [TotalPrice] >= 0 AND [DiscountPerUnit] >= 0");
            t.HasCheckConstraint("CK_OrderItems_Target", "([ItemType] = 0 AND [CourseId] IS NOT NULL) OR ([ItemType] = 1 AND [ProductId] IS NOT NULL)");
        });
    }
}

public sealed class OrderStatusHistoryConfiguration : IEntityTypeConfiguration<OrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<OrderStatusHistory> b)
    {
        b.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        b.Property(x => x.ChangedBy).HasMaxLength(256);
    }
}

public sealed class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> b)
    {
        b.Property(x => x.FullName).HasMaxLength(100).IsRequired();
        b.Property(x => x.Mobile).HasMaxLength(11).IsRequired().IsUnicode(false);
        b.Property(x => x.Email).HasMaxLength(150);
        b.Property(x => x.Notes).HasMaxLength(1000);
        b.Property(x => x.ExtraFieldsJson).HasMaxLength(8000);
        b.HasIndex(x => new { x.CourseId, x.Status, x.ReservedUntil });
        b.HasIndex(x => new { x.CourseId, x.Mobile });
        b.HasIndex(x => x.CreatedAt);
        b.HasOne(x => x.Course).WithMany(c => c.Enrollments).HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.OrderItem).WithMany().HasForeignKey(x => x.OrderItemId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> b)
    {
        b.Property(x => x.Gateway).HasMaxLength(30).IsRequired();
        b.Property(x => x.Authority).HasMaxLength(100).IsUnicode(false);
        b.HasIndex(x => x.Authority).IsUnique().HasFilter("[Authority] IS NOT NULL");
        b.Property(x => x.RefId).HasMaxLength(50).IsUnicode(false);
        b.HasIndex(x => x.RefId);
        b.HasIndex(x => new { x.Status, x.CreatedAt });
        b.Property(x => x.CardPan).HasMaxLength(30).IsUnicode(false);
        b.Property(x => x.GatewayMessage).HasMaxLength(500);
        b.Property(x => x.CallbackStatus).HasMaxLength(10).IsUnicode(false);
        b.ToTable(t => t.HasCheckConstraint("CK_PaymentTransactions_Amount", "[Amount] > 0 AND [GatewayAmount] > 0"));
    }
}
