using Notification.API.Clients.Customer.Dtos;
using Notification.API.Clients.Customer.Interfaces;

namespace Notification.API.Clients.Customer;

public class CustomerApiClient(HttpClient httpClient, ILogger<CustomerApiClient> logger)
    : ICustomerApiClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<CustomerApiClient> _logger = logger;

    public async Task<CustomerDto?> GetCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<CustomerDto>(
                $"/api/customers/{customerId}",
                cancellationToken
            );

            return response;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Customer {CustomerId} not found in Customer Service.", customerId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching customer {CustomerId}", customerId);
            return null;
        }
    }
}
