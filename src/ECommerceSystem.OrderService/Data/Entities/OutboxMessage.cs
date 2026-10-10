namespace ECommerceSystem.OrderService.Data.Entities;

/// <summary>
/// Transactional outbox row. Saved in the SAME transaction as the order change, then published
/// to RabbitMQ (with publisher confirms) by the background OutboxPublisher, which sets PublishedAtUtc.
/// Id is also the message's MessageId, so consumers can deduplicate a re-published message.
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; set; }

    /// <summary>Event/command type name, e.g. "OrderPlaced".</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>RabbitMQ routing key, e.g. "order.placed".</summary>
    public string RoutingKey { get; set; } = string.Empty;

    /// <summary>JSON body of the message.</summary>
    public string Payload { get; set; } = string.Empty;

    public string CorrelationId { get; set; } = string.Empty;

    public DateTime OccurredAtUtc { get; set; }

    /// <summary>Null until RabbitMQ confirmed the publish.</summary>
    public DateTime? PublishedAtUtc { get; set; }

    /// <summary>Number of publish attempts (for diagnostics and back-off).</summary>
    public int Attempts { get; set; }

    public string? LastError { get; set; }
}