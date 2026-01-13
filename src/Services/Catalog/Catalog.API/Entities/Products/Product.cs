using Catalog.API.Entities.Categories;

namespace Catalog.API.Entities.Products;

public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Sku { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public bool IsActive { get; set; } = true;
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    public Guid StoreId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<ProductImage> Images { get; set; } = new();
    public List<ProductAttribute> Attributes { get; set; } = new();
}
