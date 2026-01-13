namespace Catalog.API.Entities.Products;

public class ProductAttribute
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
}
