using System.Text;
using ECommerceSystem.Shared.Correlation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ECommerceSystem.Shared.Messaging;

/// <summary>
/// Base class for every RabbitMQ consumer (a hosted BackgroundService).
///
/// - Manual acknowledgements (autoAck: false) and BasicQos prefetch.
/// - Restores the correlation id (BasicProperties.CorrelationId / X-Correlation-ID header) into the
///   accessor and a logging scope, so the consumer's logs and HTTP calls carry the same id.
/// - Success                     -> BasicAck.
/// - PoisonMessageException       -> BasicNack(requeue: false) -> RabbitMQ dead-letters to "&lt;queue&gt;.dlq".
/// - Any other exception          -> transient: republished to the SAME queue (default exchange) with
///                                   x-retry-count + 1 after 1s/2s/4s, original acked.
/// - Retries exhausted            -> BasicNack(requeue: false) -> "&lt;queue&gt;.dlq".
/// Derived classes implement idempotency (ProcessedMessages) inside HandleAsync.
/// </summary>
public abstract class RabbitMqConsumerBase : BackgroundService
{
    private readonly IRabbitMqConnectionProvider _connectionProvider;
    private readonly IMessagePublisher _publisher;
    private readonly ICorrelationIdAccessor _correlationIdAccessor;
    private readonly RabbitMqOptions _options;
    private IModel? _channel;
    private string? _consumerTag;
    private CancellationToken _stoppingToken;

    protected RabbitMqConsumerBase(
        IRabbitMqConnectionProvider connectionProvider,
        IMessagePublisher publisher,
        ICorrelationIdAccessor correlationIdAccessor,
        IOptions<RabbitMqOptions> options,
        ILogger logger)
    {
        _connectionProvider = connectionProvider;
        _publisher = publisher;
        _correlationIdAccessor = correlationIdAccessor;
        _options = options.Value;
        Logger = logger;
    }

    protected ILogger Logger { get; }

    /// <summary>The queue this consumer reads from.</summary>
    protected abstract string QueueName { get; }

    /// <summary>
    /// Processes one message. Return normally to ack. Throw PoisonMessageException for messages that can
    /// never succeed; throw anything else for transient failures (they are retried).
    /// </summary>
    protected abstract Task HandleAsync(ConsumedMessage message, CancellationToken cancellationToken);

    /// <summary>Declares the topology this consumer needs (idempotent). Override for extra queues.</summary>
    protected virtual void DeclareTopology(IModel channel) => RabbitMqTopology.DeclareCoreTopology(channel);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _stoppingToken = stoppingToken;

        // Do not block application startup while connecting.
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                StartConsuming();
                break;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(
                    "Cannot start consuming from {Queue}: {Error}. Is RabbitMQ running? Retrying in 5 seconds.",
                    QueueName,
                    ex.Message);

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
    }

    private void StartConsuming()
    {
        var channel = _connectionProvider.GetConnection().CreateModel();
        DeclareTopology(channel);
        channel.BasicQos(prefetchSize: 0, prefetchCount: (ushort)Math.Clamp(_options.PrefetchCount, 1, ushort.MaxValue), global: false);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += OnMessageReceivedAsync;

        _consumerTag = channel.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);
        _channel = channel;

        Logger.LogInformation(
            "Consuming from queue {Queue} (manual ack, prefetch {Prefetch}, max retries {MaxRetries}, dead-letter queue {DeadLetterQueue}).",
            QueueName,
            _options.PrefetchCount,
            _options.MaxRetryCount,
            RabbitMqTopology.DeadLetterQueueFor(QueueName));
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs delivery)
    {
        var channel = ((IBasicConsumer)sender).Model;
        var properties = delivery.BasicProperties;

        var correlationId = MessageHeaders.GetCorrelationId(properties) ?? CorrelationIdGenerator.NewId();
        var retryCount = MessageHeaders.GetRetryCount(properties);
        var routingKey = MessageHeaders.GetStringHeader(properties, MessageHeaders.OriginalRoutingKey) ?? delivery.RoutingKey;
        var messageType = properties?.Type ?? MessageHeaders.GetStringHeader(properties, MessageHeaders.MessageType) ?? string.Empty;
        var body = Encoding.UTF8.GetString(delivery.Body.ToArray());
        var hasMessageId = Guid.TryParse(properties?.MessageId, out var messageId);

        _correlationIdAccessor.CorrelationId = correlationId;
        using var scope = Logger.BeginScope(CorrelationConstants.LogScopeFormat, correlationId);

        try
        {
            if (!hasMessageId)
            {
                throw new PoisonMessageException("The message has no valid MessageId property, so it cannot be processed idempotently.");
            }

            var message = new ConsumedMessage(messageId, messageType, routingKey, body, correlationId, retryCount, delivery.Redelivered);

            Logger.LogInformation(
                "Received {MessageType} {MessageId} from {Queue} (routing key {RoutingKey}, retry {RetryCount}).",
                messageType,
                messageId,
                QueueName,
                routingKey,
                retryCount);

            await HandleAsync(message, _stoppingToken);

            channel.BasicAck(delivery.DeliveryTag, multiple: false);
            Logger.LogInformation("Acked {MessageType} {MessageId} on {Queue}.", messageType, messageId, QueueName);
        }
        catch (PoisonMessageException ex)
        {
            Logger.LogError(
                ex,
                "Poison message on {Queue} ({Reason}). Rejecting without requeue -> dead-letter queue {DeadLetterQueue}.",
                QueueName,
                ex.Message,
                RabbitMqTopology.DeadLetterQueueFor(QueueName));

            channel.BasicNack(delivery.DeliveryTag, multiple: false, requeue: false);
        }
        catch (OperationCanceledException) when (_stoppingToken.IsCancellationRequested)
        {
            // Service is stopping: give the message back to the queue untouched.
            channel.BasicNack(delivery.DeliveryTag, multiple: false, requeue: true);
        }
        catch (Exception ex)
        {
            await HandleTransientFailureAsync(channel, delivery, messageId, messageType, routingKey, body, correlationId, retryCount, ex);
        }
    }

    private async Task HandleTransientFailureAsync(
        IModel channel,
        BasicDeliverEventArgs delivery,
        Guid messageId,
        string messageType,
        string originalRoutingKey,
        string body,
        string correlationId,
        int retryCount,
        Exception error)
    {
        if (retryCount >= _options.MaxRetryCount)
        {
            Logger.LogError(
                error,
                "Processing {MessageType} {MessageId} failed and all {MaxRetries} retries are exhausted. Dead-lettering to {DeadLetterQueue}.",
                messageType,
                messageId,
                _options.MaxRetryCount,
                RabbitMqTopology.DeadLetterQueueFor(QueueName));

            channel.BasicNack(delivery.DeliveryTag, multiple: false, requeue: false);
            return;
        }

        var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));

        Logger.LogWarning(
            error,
            "Processing {MessageType} {MessageId} failed (retry {NextRetry} of {MaxRetries} in {DelaySeconds}s): {Error}",
            messageType,
            messageId,
            retryCount + 1,
            _options.MaxRetryCount,
            delay.TotalSeconds,
            error.Message);

        try
        {
            await Task.Delay(delay, _stoppingToken);

            // Republish a copy to THIS queue only (default exchange, routing key = queue name),
            // same MessageId and CorrelationId, incremented x-retry-count. Then ack the original.
            _publisher.Publish(new OutgoingMessage(messageId, messageType, QueueName, body, correlationId)
            {
                Exchange = string.Empty,
                RetryCount = retryCount + 1,
                OriginalRoutingKey = originalRoutingKey
            });

            channel.BasicAck(delivery.DeliveryTag, multiple: false);
        }
        catch (Exception republishError)
        {
            Logger.LogError(
                republishError,
                "Could not schedule a retry for {MessageId}; returning it to {Queue}.",
                messageId,
                QueueName);

            channel.BasicNack(delivery.DeliveryTag, multiple: false, requeue: true);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_channel is { IsOpen: true })
            {
                if (_consumerTag is not null)
                {
                    _channel.BasicCancel(_consumerTag);
                }

                _channel.Close();
            }
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Error while stopping the consumer of {Queue}.", QueueName);
        }

        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}