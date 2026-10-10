using ECommerceSystem.OrderService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerceSystem.OrderService.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders", table =>
        {
            table.HasCheckConstraint("CK_Orders_TotalAmount_NonNegative", "[TotalAmount] >= 0");
            table.HasCheckConstraint("CK_Orders_CardLast4_Length", "LEN([CardLast4]) = 4");
        });

        builder.HasKey(o => o.Id);

        // Guid ids are created by the application (Guid.CreateVersion7()), never by SQL Server.
        builder.Property(o => o.Id)
            .ValueGeneratedNever();

        builder.Property(o => o.CustomerName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(o => o.CustomerEmail)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(o => o.TotalAmount)
            .HasPrecision(18, 2);

        builder.Property(o => o.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(o => o.CardLast4)
            .IsRequired()
            .HasMaxLength(4);

        builder.Property(o => o.PaymentToken)
            .HasMaxLength(32);

        builder.Property(o => o.FailureReason)
            .HasMaxLength(500);

        builder.Property(o => o.CorrelationId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(o => o.SimulateFailureAfterPayment)
            .IsRequired();

        builder.Property(o => o.CreatedAtUtc)
            .IsRequired();

        builder.Property(o => o.UpdatedAtUtc)
            .IsRequired();

        builder.Property(o => o.RowVersion)
            .IsRowVersion();

        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(o => o.StatusHistory)
            .WithOne()
            .HasForeignKey(h => h.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(o => o.CustomerEmail);
        builder.HasIndex(o => o.Status);
        builder.HasIndex(o => o.CorrelationId);
        builder.HasIndex(o => o.CreatedAtUtc);
    }
}