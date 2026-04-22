using CustomerApi.Application.DTOs;

namespace CustomerApi.Application.Interfaces;

public interface ICustomerService
{
    Task<CustomerDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<CustomerDto>> GetAllAsync(string? firstNameFilter, int page, int pageSize, CancellationToken ct = default);
    Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken ct = default);
    Task<CustomerDto> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
