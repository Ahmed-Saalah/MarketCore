namespace Notification.API.Clients.Customer.Dtos;

public record CustomerDto(
    Guid Id,
    string UserName,
    string Email,
    string DisplayName,
    string PhoneNumber,
    List<AddressDto> Addresses
);

public record AddressDto(
    Guid Id,
    string Street,
    string City,
    string State,
    string Country,
    string ZipCode,
    bool IsDefault
);
