using Azma.Domain.Common;

namespace Azma.Domain.Catalog;

public enum ProductType
{
    Physical = 0,
    Digital = 1,
}

public class Product : AuditableEntity
{
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Sku { get; set; } = "";
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public string? ImagePath { get; set; }
    public ProductType ProductType { get; set; }

    public int? CategoryId { get; set; }
    public ProductCategory? Category { get; set; }

    /// <summary>Prices in Toman.</summary>
    public long Price { get; set; }
    public long? DiscountPrice { get; set; }

    /// <summary>Null means stock is not tracked (always available while IsAvailable).</summary>
    public int? Stock { get; set; }
    public bool IsAvailable { get; set; } = true;
    public PublishStatus Status { get; set; } = PublishStatus.Draft;
    public bool IsFeatured { get; set; }
    public int SortOrder { get; set; }

    public List<ProductImage> Images { get; set; } = [];

    public long FinalPrice => Money.FinalPrice(Price, DiscountPrice);

    public bool IsPurchasable(int quantity = 1)
        => Status == PublishStatus.Published && IsAvailable && (Stock is null || Stock >= quantity);
}

public class ProductImage : Entity
{
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public string Path { get; set; } = "";
    public int SortOrder { get; set; }
}
