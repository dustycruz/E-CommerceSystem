using System.Text.Json;
using ECommerceSystem.Shared.Events;

namespace ECommerceSystem.Shared.Messaging;

/// <summary>
/// A received message as seen by a consumer. MessageType is the event name (e.g. "OrderPlaced");
/// RoutingKey is the ORIGINAL routing key (preserved across retries).
/// </summary>
public sealed record ConsumedMessage(
    Guid MessageId,
    string MessageType,
    string RoutingKey,
    string Body,
    string CorrelationId,
    int RetryCount,
    bool Redelivered)
{
    /// <summary>
    /// Deserializes the body with the strict event JSON settings.
    /// Invalid or incomplete JSON is not retryable: it throws PoisonMessageException (straight to the DLQ).
    /// </summary>
    public T Deserialize<T>() where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(Body, EventJson.Options)
                ?? throw new PoisonMessageException($"Message {MessageId} has an empty (null) body for {typeof(T).Name}.");
        }
        catch (JsonException ex)
        {
            throw new PoisonMessageException($"Message {MessageId} is not a valid {typeof(T).Name}: {ex.Message}", ex);
        }
    }
}