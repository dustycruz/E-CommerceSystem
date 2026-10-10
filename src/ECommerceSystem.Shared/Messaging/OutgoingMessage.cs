namespace ECommerceSystem.Shared.Messaging;

/// <summary>
/// A message ready to be published. Body is the JSON text (the outbox stores exactly this).
/// Exchange defaults to "ecommerce.events"; retries use the default exchange ("") with RoutingKey = queue name.
/// </summary>
public sealed record OutgoingMessage(
    Guid MessageId,
    string MessageType,
    string RoutingKey,
    string Body,
    string CorrelationId)
{
    public string Exchange { get; init; } = RabbitMqTopology.EventsExchange;

    public int RetryCount { get; init; }

    /// <summary>The routing key the message was originally published with (kept across retries).</summary>
    public string? OriginalRoutingKey { get; init; }

    public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;
}