using ECommerceSystem.Shared.Events;
using RabbitMQ.Client;

namespace ECommerceSystem.Shared.Messaging;

/// <summary>
/// The complete RabbitMQ topology, declared idempotently (declaring an existing exchange/queue with the
/// same arguments is a no-op). Every queue dead-letters to "ecommerce.dlx" with its own name as routing
/// key, and "ecommerce.dlx" routes that key to "&lt;queue&gt;.dlq".
/// </summary>
public static class RabbitMqTopology
{
    public const string EventsExchange = "ecommerce.events";
    public const string DeadLetterExchange = "ecommerce.dlx";

    public const string InventoryOrderPlacedQueue = "inventory.order-placed";
    public const string InventoryReleaseStockQueue = "inventory.release-stock";
    public const string OrderInventoryResultsQueue = "order.inventory-results";
    public const string NotificationAllQueue = "notification.all";

    public static string DeadLetterQueueFor(string queueName) => queueName + ".dlq";

    public static void DeclareExchanges(IModel channel)
    {
        channel.ExchangeDeclare(EventsExchange, ExchangeType.Topic, durable: true, autoDelete: false, arguments: null);
        channel.ExchangeDeclare(DeadLetterExchange, ExchangeType.Direct, durable: true, autoDelete: false, arguments: null);
    }

    /// <summary>
    /// Declares a durable queue bound to "ecommerce.events" with the given routing keys,
    /// plus its dead-letter queue "&lt;queue&gt;.dlq".
    /// </summary>
    public static void DeclareQueueWithDeadLetter(IModel channel, string queueName, params string[] routingKeys)
    {
        DeclareExchanges(channel);

        var deadLetterQueue = DeadLetterQueueFor(queueName);
        channel.QueueDeclare(deadLetterQueue, durable: true, exclusive: false, autoDelete: false, arguments: null);
        channel.QueueBind(deadLetterQueue, DeadLetterExchange, queueName, arguments: null);

        var arguments = new Dictionary<string, object>
        {
            ["x-dead-letter-exchange"] = DeadLetterExchange,
            ["x-dead-letter-routing-key"] = queueName
        };

        channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false, arguments: arguments);

        foreach (var routingKey in routingKeys)
        {
            channel.QueueBind(queueName, EventsExchange, routingKey, arguments: null);
        }
    }

    /// <summary>
    /// The three core saga queues. Declared by Order AND Inventory at startup, so no event published by one
    /// service is ever unroutable because the other service has not started yet.
    /// </summary>
    public static void DeclareCoreTopology(IModel channel)
    {
        DeclareQueueWithDeadLetter(channel, InventoryOrderPlacedQueue,
            EventRoutingKeys.OrderPlaced,
            EventRoutingKeys.OrderConfirmed);

        DeclareQueueWithDeadLetter(channel, InventoryReleaseStockQueue,
            EventRoutingKeys.ReleaseStock);

        DeclareQueueWithDeadLetter(channel, OrderInventoryResultsQueue,
            EventRoutingKeys.StockReserved,
            EventRoutingKeys.StockReservationFailed);
    }

    /// <summary>Bonus Notification Service queue: receives every event ("#").</summary>
    public static void DeclareNotificationTopology(IModel channel)
    {
        DeclareCoreTopology(channel);
        DeclareQueueWithDeadLetter(channel, NotificationAllQueue, "#");
    }
}