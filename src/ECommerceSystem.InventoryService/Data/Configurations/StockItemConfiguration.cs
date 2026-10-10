using ECommerceSystem.InventoryService.Data.Entities;
using ECommerceSystem.InventoryService.Data.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerceSystem.InventoryService.Data.Configurations;

public class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    public void Configure(EntityTypeBuilder<StockItem> builder)
    {
        builder.ToTable("StockItems", table =>
            table.HasCheckConstraint(
                "CK_StockItems_Quantities",
                "[QuantityOnHand] >= 0 AND [QuantityReserved] >= 0 AND [QuantityReserved] <= [QuantityOnHand]"));

        builder.HasKey(s => s.Id);

        builder.Property(s => s.ProductId)
            .IsRequired();

        builder.HasIndex(s => s.ProductId)
            .IsUnique();

        builder.Property(s => s.Sku)
            .IsRequired()
            .HasMaxLength(32);

        builder.HasIndex(s => s.Sku)
            .IsUnique();

        builder.Property(s => s.QuantityOnHand)
            .IsRequired();

        builder.Property(s => s.QuantityReserved)
            .IsRequired();

        // rowversion: SQL Server changes it on every UPDATE; EF adds it to the WHERE clause
        // and throws DbUpdateConcurrencyException if another transaction changed the row first.
        builder.Property(s => s.RowVersion)
            .IsRowVersion();

        builder.Property(s => s.UpdatedAtUtc)
            .IsRequired();

        builder.Ignore(s => s.QuantityAvailable);

        builder.HasData(InventorySeedData.StockItems);
    }
}