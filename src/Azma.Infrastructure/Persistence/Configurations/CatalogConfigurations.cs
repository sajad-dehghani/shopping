using Azma.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Azma.Infrastructure.Persistence.Configurations;

public sealed class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> b)
    {
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(150).IsRequired();
        b.HasIndex(x => x.Slug).IsUnique();
        b.HasIndex(x => new { x.Status, x.IsFeatured, x.SortOrder });
        b.Property(x => x.ShortDescription).HasMaxLength(500);
        b.Property(x => x.Description).HasMaxLength(20000);
        b.Property(x => x.Syllabus).HasMaxLength(10000);
        b.Property(x => x.Prerequisites).HasMaxLength(4000);
        b.Property(x => x.RegistrationTerms).HasMaxLength(4000);
        b.Property(x => x.CoverImagePath).HasMaxLength(300);
        b.Property(x => x.Duration).HasMaxLength(100);
        b.Property(x => x.Location).HasMaxLength(500);
        b.Property(x => x.OnlinePlatform).HasMaxLength(200);
        b.Property(x => x.SupportPhone).HasMaxLength(30);
        b.Ignore(x => x.FinalPrice);
        b.Ignore(x => x.SyllabusItems);

        b.HasOne(x => x.Category).WithMany(c => c.Courses).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.Instructor).WithMany(i => i.Courses).HasForeignKey(x => x.InstructorId).OnDelete(DeleteBehavior.SetNull);
        b.HasMany(x => x.Images).WithOne(i => i.Course).HasForeignKey(i => i.CourseId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Schedules).WithOne(i => i.Course).HasForeignKey(i => i.CourseId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.RegistrationFields).WithOne(i => i.Course).HasForeignKey(i => i.CourseId).OnDelete(DeleteBehavior.Cascade);

        b.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Courses_Price", "[Price] >= 0");
            t.HasCheckConstraint("CK_Courses_DiscountPrice", "[DiscountPrice] IS NULL OR [DiscountPrice] >= 0");
            t.HasCheckConstraint("CK_Courses_Capacity", "[Capacity] IS NULL OR [Capacity] > 0");
        });
    }
}

public sealed class CourseImageConfiguration : IEntityTypeConfiguration<CourseImage>
{
    public void Configure(EntityTypeBuilder<CourseImage> b)
    {
        b.Property(x => x.Path).HasMaxLength(300).IsRequired();
        b.Property(x => x.Caption).HasMaxLength(200);
    }
}

public sealed class CourseScheduleConfiguration : IEntityTypeConfiguration<CourseSchedule>
{
    public void Configure(EntityTypeBuilder<CourseSchedule> b) => b.Property(x => x.Note).HasMaxLength(200);
}

public sealed class CourseRegistrationFieldConfiguration : IEntityTypeConfiguration<CourseRegistrationField>
{
    public void Configure(EntityTypeBuilder<CourseRegistrationField> b)
    {
        b.Property(x => x.Label).HasMaxLength(150).IsRequired();
        b.Property(x => x.Options).HasMaxLength(2000);
        b.Ignore(x => x.OptionItems);
    }
}

public sealed class CourseCategoryConfiguration : IEntityTypeConfiguration<CourseCategory>
{
    public void Configure(EntityTypeBuilder<CourseCategory> b)
    {
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(120).IsRequired();
        b.HasIndex(x => x.Slug).IsUnique();
    }
}

public sealed class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> b)
    {
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(120).IsRequired();
        b.HasIndex(x => x.Slug).IsUnique();
    }
}

public sealed class InstructorConfiguration : IEntityTypeConfiguration<Instructor>
{
    public void Configure(EntityTypeBuilder<Instructor> b)
    {
        b.Property(x => x.FullName).HasMaxLength(150).IsRequired();
        b.Property(x => x.Title).HasMaxLength(200);
        b.Property(x => x.Bio).HasMaxLength(4000);
        b.Property(x => x.PhotoPath).HasMaxLength(300);
    }
}

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(150).IsRequired();
        b.Property(x => x.Sku).HasMaxLength(50).IsRequired();
        b.HasIndex(x => x.Slug).IsUnique();
        b.HasIndex(x => x.Sku).IsUnique();
        b.HasIndex(x => new { x.Status, x.IsFeatured, x.SortOrder });
        b.Property(x => x.ShortDescription).HasMaxLength(500);
        b.Property(x => x.Description).HasMaxLength(20000);
        b.Property(x => x.ImagePath).HasMaxLength(300);
        b.Ignore(x => x.FinalPrice);
        b.HasOne(x => x.Category).WithMany(c => c.Products).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.SetNull);
        b.HasMany(x => x.Images).WithOne(i => i.Product).HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Cascade);
        b.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Products_Price", "[Price] >= 0");
            t.HasCheckConstraint("CK_Products_DiscountPrice", "[DiscountPrice] IS NULL OR [DiscountPrice] >= 0");
            t.HasCheckConstraint("CK_Products_Stock", "[Stock] IS NULL OR [Stock] >= 0");
        });
    }
}

public sealed class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> b) => b.Property(x => x.Path).HasMaxLength(300).IsRequired();
}
