using CustomerApi.Application.Interfaces;
using CustomerApi.Domain.Interfaces;
using CustomerApi.Infrastructure.Caching;
using CustomerApi.Infrastructure.Data;
using CustomerApi.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CustomerApi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddStackExchangeRedisCache(options =>
            options.Configuration = configuration.GetConnectionString("Redis"));

        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ICustomerCache, CustomerCache>();

        return services;
    }
}
