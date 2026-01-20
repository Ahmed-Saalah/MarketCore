namespace Search.API.Models;

public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? PictureUrl { get; set; }
    public Guid StoreId { get; set; }
    public bool IsActive { get; set; }
}
