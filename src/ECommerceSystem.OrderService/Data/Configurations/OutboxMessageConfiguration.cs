using ECommerceSystem.OrderService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerceSystem.OrderService.Data.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .ValueGeneratedNever();

        builder.Property(m => m.Type)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(m => m.RoutingKey)
            .IsRequired()
            .HasMaxLength(100);

        // nvarchar(max): JSON body of the event.
        builder.Property(m => m.Payload)
            .IsRequired();

        builder.Property(m => m.CorrelationId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(m => m.OccurredAtUtc)
            .IsRequired();

        builder.Property(m => m.Attempts)
            .IsRequired();

        builder.Property(m => m.LastError)
            .HasMaxLength(2000);

        // The OutboxPublisher polls "WHERE PublishedAtUtc IS NULL ORDER BY OccurredAtUtc".
        builder.HasIndex(m => new { m.PublishedAtUtc, m.OccurredAtUtc });
    }
}