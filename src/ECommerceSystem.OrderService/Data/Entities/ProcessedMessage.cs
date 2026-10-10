namespace ECommerceSystem.OrderService.Data.Entities;

/// <summary>
/// Idempotency record for consumed RabbitMQ messages (inserted in the same transaction as the saga step).
/// </summary>
public class ProcessedMessage
{
    public Guid MessageId { get; set; }

    /// <summary>Name of the consumer, e.g. "order.inventory-results".</summary>
    public string Consumer { get; set; } = string.Empty;

    public DateTime ProcessedAtUtc { get; set; }
}