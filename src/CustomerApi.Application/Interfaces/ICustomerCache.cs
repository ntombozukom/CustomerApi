using CustomerApi.Domain.Entities;

namespace CustomerApi.Application.Interfaces;

public interface ICustomerCache
{
    Task<Customer?> GetAsync(Guid id, CancellationToken ct = default);
    Task SetAsync(Customer customer, CancellationToken ct = default);
    Task RemoveAsync(Guid id, CancellationToken ct = default);
}
