using ECommerceSystem.OrderService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerceSystem.OrderService.Data.Configurations;

public class ProcessedMessageConfiguration : IEntityTypeConfiguration<ProcessedMessage>
{
    public void Configure(EntityTypeBuilder<ProcessedMessage> builder)
    {
        builder.ToTable("ProcessedMessages");

        builder.HasKey(m => m.MessageId);

        builder.Property(m => m.MessageId)
            .ValueGeneratedNever();

        builder.Property(m => m.Consumer)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.ProcessedAtUtc)
            .IsRequired();
    }
}