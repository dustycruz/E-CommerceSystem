using RabbitMQ.Client;

namespace ECommerceSystem.Shared.Messaging;

/// <summary>Provides the single, long-lived RabbitMQ connection of a service.</summary>
public interface IRabbitMqConnectionProvider
{
    /// <summary>Returns the connection, creating it on first use. Throws if the broker is unreachable.</summary>
    IConnection GetConnection();
}