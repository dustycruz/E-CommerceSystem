namespace ECommerceSystem.InventoryService.Data.Entities;

/// <summary>
/// Idempotency record: a RabbitMQ message id that has already been processed by a consumer.
/// Inserted in the SAME SaveChanges (transaction) as the business change, so a redelivered
/// message is detected (primary key violation / existence check) and acknowledged without side effects.
/// </summary>
public class ProcessedMessage
{
    public Guid MessageId { get; set; }

    /// <summary>Name of the consumer that processed it, e.g. "inventory.order-placed".</summary>
    public string Consumer { get; set; } = string.Empty;

    public DateTime ProcessedAtUtc { get; set; }
}