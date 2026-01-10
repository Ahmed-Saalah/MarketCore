namespace Customer.API.Entities;

public class Customer
{
    public Guid Id { get; set; }
    public int IdentityId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public List<Address> Addresses { get; set; } = new();
}
