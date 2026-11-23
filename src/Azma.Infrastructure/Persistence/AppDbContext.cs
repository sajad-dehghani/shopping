using Azma.Application.Abstractions;
using Azma.Domain.Auditing;
using Azma.Domain.Catalog;
using Azma.Domain.Content;
using Azma.Domain.Sales;
using Azma.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Azma.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options), IAppDbContext
{
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseCategory> CourseCategories => Set<CourseCategory>();
    public DbSet<CourseImage> CourseImages => Set<CourseImage>();
    public DbSet<CourseSchedule> CourseSchedules => Set<CourseSchedule>();
    public DbSet<CourseRegistrationField> CourseRegistrationFields => Set<CourseRegistrationField>();
    public DbSet<Instructor> Instructors => Set<Instructor>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<PaymentReceipt> PaymentReceipts => Set<PaymentReceipt>();
    public DbSet<Azma.Domain.Support.Ticket> Tickets => Set<Azma.Domain.Support.Ticket>();
    public DbSet<Azma.Domain.Support.TicketMessage> TicketMessages => Set<Azma.Domain.Support.TicketMessage>();
    public DbSet<SiteSettings> SiteSettings => Set<SiteSettings>();
    public DbSet<FaqItem> FaqItems => Set<FaqItem>();
    public DbSet<FeatureItem> FeatureItems => Set<FeatureItem>();
    public DbSet<HeroSlide> HeroSlides => Set<HeroSlide>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<AdminAuditLog> AdminAuditLogs => Set<AdminAuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // All DateTime values are stored as UTC; mark them as such when read back.
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<NullableUtcConverter>();
    }

    private sealed class UtcConverter() : ValueConverter<DateTime, DateTime>(
        v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    private sealed class NullableUtcConverter() : ValueConverter<DateTime?, DateTime?>(
        v => v == null ? v : (v.Value.Kind == DateTimeKind.Utc ? v : v.Value.ToUniversalTime()),
        v => v == null ? v : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc));
}

/// <summary>Adapts EF's pooled IDbContextFactory to the application abstraction.</summary>
public sealed class AppDbContextFactory(IDbContextFactory<AppDbContext> inner) : IAppDbContextFactory
{
    public async Task<IAppDbContext> CreateAsync(CancellationToken cancellationToken = default)
        => await inner.CreateDbContextAsync(cancellationToken);
}
