using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace ECommerceSystem.Shared.Messaging;

/// <summary>
/// Singleton that owns ONE connection per service (channels are cheap, connections are not).
/// DispatchConsumersAsync = true is required by AsyncEventingBasicConsumer.
/// Automatic recovery reconnects the connection, channels and consumers after a broker restart.
/// </summary>
public sealed class RabbitMqConnectionProvider(
    IOptions<RabbitMqOptions> options,
    IHostEnvironment environment,
    ILogger<RabbitMqConnectionProvider> logger) : IRabbitMqConnectionProvider, IDisposable
{
    private readonly object _sync = new();
    private volatile IConnection? _connection;
    private bool _disposed;

    public IConnection GetConnection()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var existing = _connection;
        if (existing is not null)
        {
            // While the broker is down the existing connection is recovering by itself - never create a second one.
            return existing;
        }

        lock (_sync)
        {
            if (_connection is not null)
            {
                return _connection;
            }

            var factory = CreateFactory();
            logger.LogInformation("Connecting to RabbitMQ at {Endpoint} as {ClientName}.", DescribeEndpoint(), factory.ClientProvidedName);

            _connection = factory.CreateConnection();

            logger.LogInformation("Connected to RabbitMQ at {Endpoint}.", DescribeEndpoint());
            return _connection;
        }
    }

    private ConnectionFactory CreateFactory()
    {
        var settings = options.Value;

        var factory = new ConnectionFactory
        {
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
            RequestedHeartbeat = TimeSpan.FromSeconds(30),
            ClientProvidedName = environment.ApplicationName
        };

        if (!string.IsNullOrWhiteSpace(settings.Uri))
        {
            // amqps:// enables TLS automatically (CloudAMQP).
            factory.Uri = new Uri(settings.Uri);
        }
        else
        {
            factory.HostName = settings.HostName;
            factory.Port = settings.Port;
            factory.VirtualHost = settings.VirtualHost;

            if (!string.IsNullOrWhiteSpace(settings.UserName))
            {
                factory.UserName = settings.UserName;
            }

            if (!string.IsNullOrWhiteSpace(settings.Password))
            {
                factory.Password = settings.Password;
            }
        }

        return factory;
    }

    /// <summary>Host/port/vhost only - never logs the password or the full URI.</summary>
    private string DescribeEndpoint()
    {
        var settings = options.Value;
        if (!string.IsNullOrWhiteSpace(settings.Uri) && System.Uri.TryCreate(settings.Uri, UriKind.Absolute, out var uri))
        {
            return $"{uri.Scheme}://{uri.Host}:{(uri.IsDefaultPort ? (uri.Scheme == "amqps" ? 5671 : 5672) : uri.Port)}{uri.AbsolutePath}";
        }

        return $"amqp://{settings.HostName}:{settings.Port}{settings.VirtualHost}";
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            _connection?.Close(TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Error while closing the RabbitMQ connection.");
        }

        _connection?.Dispose();
    }
}