using ECommerceSystem.InventoryService.Services;
using ECommerceSystem.Shared.Correlation;
using ECommerceSystem.Shared.Events;
using ECommerceSystem.Shared.Messaging;
using Microsoft.Extensions.Options;

namespace ECommerceSystem.InventoryService.Consumers;

/// <summary>
/// Consumes queue "inventory.release-stock" (bound to order.release-stock).
/// ReleaseStock is the saga's COMPENSATION command: the order's Reserved units become available again.
/// Idempotent: releasing twice (or releasing an order that holds nothing) changes nothing.
/// </summary>
public sealed class ReleaseStockConsumer(
    IServiceScopeFactory scopeFactory,
    IRabbitMqConnectionProvider connectionProvider,
    IMessagePublisher publisher,
    ICorrelationIdAccessor correlationIdAccessor,
    IOptions<RabbitMqOptions> options,
    ILogger<ReleaseStockConsumer> logger)
    : RabbitMqConsumerBase(connectionProvider, publisher, correlationIdAccessor, options, logger)
{
    protected override string QueueName => RabbitMqTopology.InventoryReleaseStockQueue;

    protected override async Task HandleAsync(ConsumedMessage message, CancellationToken cancellationToken)
    {
        var isReleaseStock = message.MessageType == nameof(ReleaseStock)
            || (string.IsNullOrWhiteSpace(message.MessageType) && message.RoutingKey == EventRoutingKeys.ReleaseStock);

        if (!isReleaseStock)
        {
            throw new PoisonMessageException(
                $"Unexpected message type '{message.MessageType}' (routing key '{message.RoutingKey}') on {QueueName}.");
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var reservations = scope.ServiceProvider.GetRequiredService<InventoryReservationService>();

        await reservations.HandleReleaseStockAsync(message.Deserialize<ReleaseStock>(), message, QueueName, cancellationToken);
    }
}