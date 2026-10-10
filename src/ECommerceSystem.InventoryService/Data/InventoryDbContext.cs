using ECommerceSystem.InventoryService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerceSystem.InventoryService.Data;

/// <summary>
/// EF Core context for ECommerceInventoryDb. Only the Inventory Service uses this database.
/// </summary>
public class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public DbSet<StockItem> StockItems => Set<StockItem>();

    public DbSet<StockReservation> StockReservations => Set<StockReservation>();

    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);
    }
}