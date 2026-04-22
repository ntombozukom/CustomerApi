using CustomerApi.Application.DTOs;
using CustomerApi.Application.Interfaces;
using CustomerApi.Domain.Entities;
using CustomerApi.Domain.Exceptions;
using CustomerApi.Domain.Interfaces;
using FluentValidation;

namespace CustomerApi.Application.Services;

public sealed class CustomerService(ICustomerRepository repository, ICustomerCache cache, IValidator<CreateCustomerRequest> createValidator, IValidator<UpdateCustomerRequest> updateValidator) : ICustomerService
{
    public async Task<CustomerDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var cached = await cache.GetAsync(id, ct);
        if (cached is not null)
            return ToDto(cached);

        var customer = await GetCustomerOrThrowAsync(id, ct);
        await cache.SetAsync(customer, ct);
        return ToDto(customer);
    }

    public async Task<PagedResult<CustomerDto>> GetAllAsync(string? firstNameFilter, int page, int pageSize, CancellationToken ct = default)
    {
        var filtered = await repository.GetAllAsync(firstNameFilter, ct);

        var totalCount = filtered.Count;
        var items = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToList();

        return new PagedResult<CustomerDto>(items, page, pageSize, totalCount);
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken ct = default)
    {
        await createValidator.ValidateAndThrowAsync(request, ct);

        var existing = await repository.GetByEmailAsync(request.Email, ct);
        if (existing is not null)
            throw new DuplicateEmailException(request.Email);

        var customer = new Customer
        {
            FirstName = request.FirstName,
            LastName  = request.LastName,
            Email     = request.Email,
            Age       = request.Age,
        };

        var created = await repository.CreateAsync(customer, ct);
        await cache.SetAsync(created, ct);
        return ToDto(created);
    }

    public async Task<CustomerDto> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken ct = default)
    {
        await updateValidator.ValidateAndThrowAsync(request, ct);

        var customer = await GetCustomerOrThrowAsync(id, ct);

        if (!string.Equals(customer.Email, request.Email, StringComparison.OrdinalIgnoreCase))
        {
            var existing = await repository.GetByEmailAsync(request.Email, ct);
            if (existing is not null)
                throw new DuplicateEmailException(request.Email);
        }

        customer.FirstName = request.FirstName;
        customer.LastName  = request.LastName;
        customer.Email     = request.Email;
        customer.Age       = request.Age;
        customer.UpdatedAt = DateTime.UtcNow;

        var updated = await repository.UpdateAsync(customer, ct);
        await cache.SetAsync(updated, ct);
        return ToDto(updated);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        if (!await repository.ExistsAsync(id, ct))
            throw new CustomerNotFoundException(id);

        await repository.DeleteAsync(id, ct);
        await cache.RemoveAsync(id, ct);
    }

    private async Task<Customer> GetCustomerOrThrowAsync(Guid id, CancellationToken ct) =>
        await repository.GetByIdAsync(id, ct) ?? throw new CustomerNotFoundException(id);

    private static CustomerDto ToDto(Customer c) =>
        new(c.Id, c.FirstName, c.LastName, c.Email, c.Age, c.CreatedAt, c.UpdatedAt);
}