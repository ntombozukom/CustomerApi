using CustomerApi.Domain.Entities;
using CustomerApi.Infrastructure.Encryption;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CustomerApi.Infrastructure.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options, IConfiguration configuration)
    : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var encryptionKey = configuration[EncryptionExtensions.ConfigKey]
            ?? throw new InvalidOperationException($"{EncryptionExtensions.ConfigKey} is not configured.");

        var converter = new AesEncryptionConverter(encryptionKey);

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.Property(e => e.FirstName)
                  .HasMaxLength(500)
                  .HasConversion(converter)
                  .IsRequired();

            entity.Property(e => e.LastName)
                  .HasMaxLength(500)
                  .HasConversion(converter)
                  .IsRequired();

            entity.Property(e => e.Email)
                  .HasMaxLength(500)
                  .HasConversion(converter)
                  .IsRequired();

            
            entity.Property(e => e.FirstNameHash)
                  .HasMaxLength(100)
                  .IsRequired();

            entity.Property(e => e.LastNameHash)
                  .HasMaxLength(100)
                  .IsRequired();

            entity.Property(e => e.EmailHash)
                  .HasMaxLength(100)
                  .IsRequired();

            entity.HasIndex(e => e.EmailHash)
                  .IsUnique()
                  .HasDatabaseName("ix_customers_email_hash");

            entity.HasIndex(e => e.FirstNameHash)
                  .HasDatabaseName("ix_customers_first_name_hash");

            entity.HasIndex(e => e.LastNameHash)
                  .HasDatabaseName("ix_customers_last_name_hash");

            entity.Property(e => e.Age).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.ToTable("customers");
        });
    }
}
