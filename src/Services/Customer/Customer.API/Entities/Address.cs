using System.Text.Json.Serialization;

namespace Customer.API.Entities;

public class Address
{
    public Guid Id { get; set; }
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public Guid CustomerId { get; set; }

    [JsonIgnore]
    public Customer? Customer { get; set; }
}
