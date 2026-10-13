using ECommerceSystem.OrderService.Saga;
using ECommerceSystem.Shared.Correlation;
using ECommerceSystem.Shared.Events;
using ECommerceSystem.Shared.Messaging;
using Microsoft.Extensions.Options;

namespace ECommerceSystem.OrderService.Consumers;

/// <summary>
/// Consumes queue "order.inventory-results" (bound to inventory.stock-reserved and
/// inventory.stock-reservation-failed) and hands each result to the saga orchestrator.
/// Manual ack, retries, DLQ and correlation-id restore come from RabbitMqConsumerBase.
/// The correlation id restored from the message flows into the Payment HTTP call (X-Correlation-ID).
/// </summary>
public sealed class InventoryResultsConsumer(
    IServiceScopeFactory scopeFactory,
    IRabbitMqConnectionProvider connectionProvider,
    IMessagePublisher publisher,
    ICorrelationIdAccessor correlationIdAccessor,
    IOptions<RabbitMqOptions> options,
    ILogger<InventoryResultsConsumer> logger)
    : RabbitMqConsumerBase(connectionProvider, publisher, correlationIdAccessor, options, logger)
{
    protected override string QueueName => RabbitMqTopology.OrderInventoryResultsQueue;

    protected override async Task HandleAsync(ConsumedMessage message, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var saga = scope.ServiceProvider.GetRequiredService<OrderSagaOrchestrator>();

        switch (ResolveMessageType(message))
        {
            case nameof(StockReserved):
                await saga.HandleStockReservedAsync(message.Deserialize<StockReserved>(), message, QueueName, cancellationToken);
                break;

            case nameof(StockReservationFailed):
                await saga.HandleStockReservationFailedAsync(message.Deserialize<StockReservationFailed>(), message, QueueName, cancellationToken);
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
            EventRoutingKeys.StockReserved => nameof(StockReserved),
            EventRoutingKeys.StockReservationFailed => nameof(StockReservationFailed),
            _ => string.Empty
        };
    }
}