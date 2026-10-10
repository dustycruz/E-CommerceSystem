namespace ECommerceSystem.Shared.Messaging;

/// <summary>
/// Bound from the "RabbitMq" configuration section.
/// Local: HostName/Port/UserName/Password in appsettings.Development.json (guest/guest, localhost only).
/// CloudAMQP: set Uri with "dotnet user-secrets set RabbitMq:Uri amqps://..." - Uri wins when not empty.
/// </summary>
public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; set; } = "localhost";

    public int Port { get; set; } = 5672;

    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string VirtualHost { get; set; } = "/";

    /// <summary>Full AMQP(S) URI, e.g. a CloudAMQP URL. Never commit it - use user-secrets.</summary>
    public string Uri { get; set; } = string.Empty;

    /// <summary>Unacknowledged messages a consumer may hold at once (BasicQos prefetch).</summary>
    public int PrefetchCount { get; set; } = 10;

    /// <summary>Retries of a failing message (via x-retry-count) before it is dead-lettered.</summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>How long the publisher waits for the broker confirm.</summary>
    public int PublishConfirmTimeoutSeconds { get; set; } = 5;
}