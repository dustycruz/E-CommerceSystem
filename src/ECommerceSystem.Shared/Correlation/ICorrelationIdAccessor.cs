namespace ECommerceSystem.Shared.Correlation;

/// <summary>
/// Gives access to the correlation id of the current logical operation
/// (an HTTP request, or one RabbitMQ message being consumed).
/// </summary>
public interface ICorrelationIdAccessor
{
    /// <summary>The current correlation id, or null outside of a request/message.</summary>
    string? CorrelationId { get; set; }
}