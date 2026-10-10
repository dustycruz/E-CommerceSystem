namespace ECommerceSystem.Shared.Events;

/// <summary>
/// Base of every message exchanged through RabbitMQ.
/// MessageId   - unique per message, used by consumers for idempotency (ProcessedMessages table).
/// OccurredAtUtc - when the event happened.
/// CorrelationId - the X-Correlation-ID of the request that started the saga.
/// </summary>
public abstract record IntegrationEvent
{
    public Guid MessageId { get; init; } = Guid.NewGuid();

    public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;

    public string CorrelationId { get; init; } = string.Empty;
}