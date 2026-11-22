using Azma.Domain.Auditing;
using Azma.Domain.Catalog;
using Azma.Domain.Content;
using Azma.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Azma.Application.Abstractions;

public interface IAppDbContext : IAsyncDisposable, IDisposable
{
    DbSet<Course> Courses { get; }
    DbSet<CourseCategory> CourseCategories { get; }
    DbSet<CourseImage> CourseImages { get; }
    DbSet<CourseSchedule> CourseSchedules { get; }
    DbSet<CourseRegistrationField> CourseRegistrationFields { get; }
    DbSet<Instructor> Instructors { get; }
    DbSet<Product> Products { get; }
    DbSet<ProductCategory> ProductCategories { get; }
    DbSet<ProductImage> ProductImages { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<OrderStatusHistory> OrderStatusHistories { get; }
    DbSet<Enrollment> Enrollments { get; }
    DbSet<PaymentTransaction> PaymentTransactions { get; }
    DbSet<SiteSettings> SiteSettings { get; }
    DbSet<FaqItem> FaqItems { get; }
    DbSet<FeatureItem> FeatureItems { get; }
    DbSet<ContactMessage> ContactMessages { get; }
    DbSet<AdminAuditLog> AdminAuditLogs { get; }

    DbSet<TEntity> Set<TEntity>() where TEntity : class;
    DatabaseFacade Database { get; }
    Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Creates a short-lived DbContext per unit of work. Required for Blazor Server
/// where a scoped DbContext would otherwise live for the whole circuit.
/// </summary>
public interface IAppDbContextFactory
{
    Task<IAppDbContext> CreateAsync(CancellationToken cancellationToken = default);
}
