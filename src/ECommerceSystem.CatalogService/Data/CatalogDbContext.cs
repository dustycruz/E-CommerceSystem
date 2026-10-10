using ECommerceSystem.CatalogService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerceSystem.CatalogService.Data;

/// <summary>
/// EF Core context for ECommerceCatalogDb. Only the Catalog Service uses this database.
/// </summary>
public class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
    }
}