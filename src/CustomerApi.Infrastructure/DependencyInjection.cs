using CustomerApi.Application.Interfaces;
using CustomerApi.Domain.Interfaces;
using CustomerApi.Infrastructure.Caching;
using CustomerApi.Infrastructure.Data;
using CustomerApi.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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

    public static IHost MigrateDatabase(this IHost host)
    {
        var raw = host.Services.GetRequiredService<IConfiguration>()["Database:AutoMigrate"];
        if (!bool.TryParse(raw, out var autoMigrate) || !autoMigrate)
            return host;

        using var scope = host.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();
        var db     = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            logger.LogInformation("Applying database migrations...");
            db.Database.Migrate();
            logger.LogInformation("Database migrations applied successfully.");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Failed to apply database migrations.");
            throw;
        }

        return host;
    }
}
