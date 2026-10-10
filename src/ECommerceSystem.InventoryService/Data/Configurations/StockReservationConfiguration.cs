using ECommerceSystem.InventoryService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerceSystem.InventoryService.Data.Configurations;

public class StockReservationConfiguration : IEntityTypeConfiguration<StockReservation>
{
    public void Configure(EntityTypeBuilder<StockReservation> builder)
    {
        builder.ToTable("StockReservations", table =>
            table.HasCheckConstraint("CK_StockReservations_Quantity_Positive", "[Quantity] > 0"));

        builder.HasKey(r => r.Id);

        builder.Property(r => r.OrderId)
            .IsRequired();

        builder.Property(r => r.ProductId)
            .IsRequired();

        builder.Property(r => r.Quantity)
            .IsRequired();

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.CreatedAtUtc)
            .IsRequired();

        // One reservation row per product per order: a duplicate reservation is impossible.
        builder.HasIndex(r => new { r.OrderId, r.ProductId })
            .IsUnique();

        builder.HasIndex(r => r.Status);
    }
}