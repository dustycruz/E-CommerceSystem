using ECommerceSystem.PaymentService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerceSystem.PaymentService.Data.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments", table =>
        {
            table.HasCheckConstraint("CK_Payments_Amount_Positive", "[Amount] > 0");
            table.HasCheckConstraint("CK_Payments_CardLast4_Length", "LEN([CardLast4]) = 4");
        });

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .ValueGeneratedNever();

        builder.Property(p => p.OrderId)
            .IsRequired();

        // Idempotency key: one payment per order, enforced by the database.
        builder.HasIndex(p => p.OrderId)
            .IsUnique();

        builder.Property(p => p.Amount)
            .HasPrecision(18, 2);

        builder.Property(p => p.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(p => p.CardLast4)
            .IsRequired()
            .HasMaxLength(4);

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(p => p.Status);

        builder.Property(p => p.FailureReason)
            .HasMaxLength(500);

        builder.Property(p => p.CorrelationId)
            .HasMaxLength(100);

        builder.Property(p => p.CreatedAtUtc)
            .IsRequired();

        builder.Property(p => p.UpdatedAtUtc)
            .IsRequired();

        builder.Property(p => p.RowVersion)
            .IsRowVersion();
    }
}