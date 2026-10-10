using System.Text;
using ECommerceSystem.Shared.Correlation;
using RabbitMQ.Client;

namespace ECommerceSystem.Shared.Messaging;

/// <summary>
/// Custom AMQP header names and safe readers (RabbitMQ.Client 6 returns string headers as byte[]).
/// </summary>
public static class MessageHeaders
{
    public const string CorrelationId = CorrelationConstants.HeaderName;
    public const string RetryCount = "x-retry-count";
    public const string MessageType = "x-message-type";
    public const string OriginalRoutingKey = "x-original-routing-key";

    public static int GetRetryCount(IBasicProperties? properties)
    {
        var value = GetRawHeader(properties, RetryCount);
        return value switch
        {
            int i => i,
            long l => (int)l,
            short s => s,
            byte b => b,
            byte[] bytes when int.TryParse(Encoding.UTF8.GetString(bytes), out var parsedBytes) => parsedBytes,
            string text when int.TryParse(text, out var parsedText) => parsedText,
            _ => 0
        };
    }

    /// <summary>Reads BasicProperties.CorrelationId first, then the X-Correlation-ID header.</summary>
    public static string? GetCorrelationId(IBasicProperties? properties)
    {
        if (properties is not null && CorrelationIdGenerator.IsValid(properties.CorrelationId))
        {
            return properties.CorrelationId;
        }

        var fromHeader = GetStringHeader(properties, CorrelationId);
        return CorrelationIdGenerator.IsValid(fromHeader) ? fromHeader : null;
    }

    public static string? GetStringHeader(IBasicProperties? properties, string name)
    {
        return GetRawHeader(properties, name) switch
        {
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            string text => text,
            null => null,
            var other => other.ToString()
        };
    }

    private static object? GetRawHeader(IBasicProperties? properties, string name)
    {
        if (properties?.Headers is null)
        {
            return null;
        }

        return properties.Headers.TryGetValue(name, out var value) ? value : null;
    }
}