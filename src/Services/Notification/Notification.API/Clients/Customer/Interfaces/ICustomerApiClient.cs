using Notification.API.Clients.Customer.Dtos;

namespace Notification.API.Clients.Customer.Interfaces;

public interface ICustomerApiClient
{
    Task<CustomerDto?> GetCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken = default
    );
}
