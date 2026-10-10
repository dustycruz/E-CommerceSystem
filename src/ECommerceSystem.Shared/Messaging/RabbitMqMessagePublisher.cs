using System.Text;
using System.Text.Json;
using ECommerceSystem.Shared.Correlation;
using ECommerceSystem.Shared.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace ECommerceSystem.Shared.Messaging;

/// <summary>
/// Singleton publisher on its own channel in confirm mode (ConfirmSelect).
/// An IModel is not thread-safe, so publishing is serialized with a lock.
/// After any failure the channel is closed and re-created on the next publish.
/// </summary>
public sealed class RabbitMqMessagePublisher(
    IRabbitMqConnectionProvider connectionProvider,
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqMessagePublisher> logger) : IMessagePublisher, IDisposable
{
    private readonly object _sync = new();
    private IModel? _channel;

    public void PublishEvent<TEvent>(TEvent integrationEvent, string routingKey) where TEvent : IntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var body = JsonSerializer.Serialize(integrationEvent, EventJson.Options);
        Publish(new OutgoingMessage(
            integrationEvent.MessageId,
            typeof(TEvent).Name,
            routingKey,
            body,
            integrationEvent.CorrelationId)
        {
            OccurredAtUtc = integrationEvent.OccurredAtUtc
        });
    }

    public void Publish(OutgoingMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        using var scope = logger.BeginScope(CorrelationConstants.LogScopeFormat, message.CorrelationId);

        lock (_sync)
        {
            var channel = GetOrCreateChannel();

            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";
            properties.ContentEncoding = "utf-8";
            properties.MessageId = message.MessageId.ToString();
            properties.CorrelationId = message.CorrelationId;
            properties.Type = message.MessageType;
            properties.Timestamp = new AmqpTimestamp(new DateTimeOffset(DateTime.SpecifyKind(message.OccurredAtUtc, DateTimeKind.Utc)).ToUnixTimeSeconds());
            properties.Headers = new Dictionary<string, object>
            {
                [MessageHeaders.CorrelationId] = message.CorrelationId,
                [MessageHeaders.RetryCount] = message.RetryCount,
                [MessageHeaders.MessageType] = message.MessageType,
                [MessageHeaders.OriginalRoutingKey] = message.OriginalRoutingKey ?? message.RoutingKey
            };

            try
            {
                channel.BasicPublish(
                    exchange: message.Exchange,
                    routingKey: message.RoutingKey,
                    mandatory: false,
                    basicProperties: properties,
                    body: Encoding.UTF8.GetBytes(message.Body));

                // Publisher confirm: blocks until the broker has taken responsibility for the message.
                channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(options.Value.PublishConfirmTimeoutSeconds));
            }
            catch
            {
                CloseChannel();
                throw;
            }
        }

        logger.LogInformation(
            "Published {MessageType} {MessageId} to exchange '{Exchange}' with routing key '{RoutingKey}' (retry {RetryCount}) - broker confirmed.",
            message.MessageType,
            message.MessageId,
            message.Exchange.Length == 0 ? "(default)" : message.Exchange,
            message.RoutingKey,
            message.RetryCount);
    }

    private IModel GetOrCreateChannel()
    {
        if (_channel is { IsOpen: true })
        {
            return _channel;
        }

        CloseChannel();

        var channel = connectionProvider.GetConnection().CreateModel();
        channel.ConfirmSelect();
        RabbitMqTopology.DeclareCoreTopology(channel);

        _channel = channel;
        return channel;
    }

    private void CloseChannel()
    {
        if (_channel is null)
        {
            return;
        }

        try
        {
            if (_channel.IsOpen)
            {
                _channel.Close();
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Error while closing the publisher channel.");
        }

        _channel.Dispose();
        _channel = null;
    }

    public void Dispose()
    {
        lock (_sync)
        {
            CloseChannel();
        }
    }
}