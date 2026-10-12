using ECommerceSystem.InventoryService.Services;
using ECommerceSystem.Shared.Correlation;
using ECommerceSystem.Shared.Events;
using ECommerceSystem.Shared.Messaging;
using Microsoft.Extensions.Options;

namespace ECommerceSystem.InventoryService.Consumers;

/// <summary>
/// Consumes queue "inventory.order-placed" (bound to order.placed and order.confirmed).
///  - OrderPlaced    -> reserve all lines or none, publish StockReserved / StockReservationFailed.
///  - OrderConfirmed -> commit the reservation (units leave the warehouse).
/// Manual ack, retries and DLQ are handled by RabbitMqConsumerBase.
/// </summary>
public sealed class OrderPlacedConsumer(
    IServiceScopeFactory scopeFactory,
    IRabbitMqConnectionProvider connectionProvider,
    IMessagePublisher publisher,
    ICorrelationIdAccessor correlationIdAccessor,
    IOptions<RabbitMqOptions> options,
    ILogger<OrderPlacedConsumer> logger)
    : RabbitMqConsumerBase(connectionProvider, publisher, correlationIdAccessor, options, logger)
{
    protected override string QueueName => RabbitMqTopology.InventoryOrderPlacedQueue;

    protected override async Task HandleAsync(ConsumedMessage message, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var reservations = scope.ServiceProvider.GetRequiredService<InventoryReservationService>();

        switch (ResolveMessageType(message))
        {
            case nameof(OrderPlaced):
                await reservations.HandleOrderPlacedAsync(message.Deserialize<OrderPlaced>(), message, QueueName, cancellationToken);
                break;

            case nameof(OrderConfirmed):
                await reservations.HandleOrderConfirmedAsync(message.Deserialize<OrderConfirmed>(), message, QueueName, cancellationToken);
                break;

            default:
                throw new PoisonMessageException(
                    $"Unexpected message type '{message.MessageType}' (routing key '{message.RoutingKey}') on {QueueName}.");
        }
    }

    private static string ResolveMessageType(ConsumedMessage message)
    {
        if (!string.IsNullOrWhiteSpace(message.MessageType))
        {
            return message.MessageType;
        }

        return message.RoutingKey switch
        {
            EventRoutingKeys.OrderPlaced => nameof(OrderPlaced),
            EventRoutingKeys.OrderConfirmed => nameof(OrderConfirmed),
            _ => string.Empty
        };
    }
}