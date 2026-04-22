using CustomerApi.Domain.Entities;
using CustomerApi.Domain.Exceptions;
using CustomerApi.Domain.Interfaces;
using CustomerApi.Infrastructure.Data;
using CustomerApi.Infrastructure.Encryption;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CustomerApi.Infrastructure.Repositories;

public sealed class CustomerRepository(AppDbContext db, IConfiguration configuration) : ICustomerRepository
{
    private readonly byte[] _key = EncryptionExtensions.DeriveKey(configuration[EncryptionExtensions.ConfigKey]
            ?? throw new InvalidOperationException($"{EncryptionExtensions.ConfigKey} is not configured."));

    public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
    public async Task<IReadOnlyList<Customer>> GetAllAsync(string? firstNameFilter = null, CancellationToken ct = default)
    {
        var query = db.Customers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(firstNameFilter))
        {
            var hash = firstNameFilter.ToSearchHash(_key);
            query = query.Where(c => c.FirstNameHash == hash);
        }

        return await query.OrderBy(c => c.CreatedAt).ToListAsync(ct);
    }
    public async Task<Customer?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var hash = email.ToSearchHash(_key);
        return await db.Customers.AsNoTracking()
                       .FirstOrDefaultAsync(c => c.EmailHash == hash, ct);
    }
    public async Task<Customer> CreateAsync(Customer customer, CancellationToken ct = default)
    {
        SetSearchHashes(customer);
        db.Customers.Add(customer);
        await db.SaveChangesAsync(ct);
        return customer;
    }
    public async Task<Customer> UpdateAsync(Customer customer, CancellationToken ct = default)
    {
        var cust = await db.Customers.FindAsync([customer.Id], ct)
            ?? throw new CustomerNotFoundException(customer.Id);

        cust.FirstName  = customer.FirstName;
        cust.LastName   = customer.LastName;
        cust.Email      = customer.Email;
        cust.Age        = customer.Age;
        cust.UpdatedAt  = customer.UpdatedAt;
        SetSearchHashes(cust);

        await db.SaveChangesAsync(ct);
        return cust;
    }
    public async Task DeleteAsync(Guid id, CancellationToken ct = default) =>
        await db.Customers.Where(c => c.Id == id).ExecuteDeleteAsync(ct);
    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default) =>
        await db.Customers.AnyAsync(c => c.Id == id, ct);
    private void SetSearchHashes(Customer customer)
    {
        customer.FirstNameHash = customer.FirstName.ToSearchHash(_key);
        customer.LastNameHash  = customer.LastName.ToSearchHash(_key);
        customer.EmailHash     = customer.Email.ToSearchHash(_key);
    }
}