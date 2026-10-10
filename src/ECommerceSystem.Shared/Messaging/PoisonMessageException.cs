namespace ECommerceSystem.Shared.Messaging;

/// <summary>
/// Thrown when a message can never be processed (malformed JSON, missing MessageId, unknown type, invalid data).
/// The consumer base class rejects it with BasicNack(requeue: false), so RabbitMQ dead-letters it immediately.
/// </summary>
public sealed class PoisonMessageException : Exception
{
    public PoisonMessageException(string message)
        : base(message)
    {
    }

    public PoisonMessageException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}