using System.Text.Json;
using CustomerApi.Application.Interfaces;
using CustomerApi.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace CustomerApi.Infrastructure.Caching;

public sealed class CustomerCache(IDistributedCache cache, ILogger<CustomerCache> logger) : ICustomerCache
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(30);
    private static string Key(Guid id) => $"customer:{id}";

    public async Task<Customer?> GetAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var data = await cache.GetStringAsync(Key(id), ct);
            if (data is null) return null;

            logger.LogDebug("Cache hit for customer {Id}", id);
            return JsonSerializer.Deserialize<Customer>(data);
        }
        catch (OperationCanceledException)
        {
            logger.LogDebug("Cache GET cancelled for customer {Id}", id);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache GET failed for customer {Id}", id);
            return null;
        }
    }

    public async Task SetAsync(Customer customer, CancellationToken ct = default)
    {
        try
        {
            var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = Ttl };
            await cache.SetStringAsync(Key(customer.Id), JsonSerializer.Serialize(customer), options, ct);
        }
        catch (OperationCanceledException)
        {
            logger.LogDebug("Cache SET cancelled for customer {Id}", customer.Id);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache SET failed for customer {Id}", customer.Id);
        }
    }

    public async Task RemoveAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            await cache.RemoveAsync(Key(id), ct);
        }
        catch (OperationCanceledException)
        {
            logger.LogDebug("Cache REMOVE cancelled for customer {Id}", id);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache REMOVE failed for customer {Id}", id);
        }
    }
}