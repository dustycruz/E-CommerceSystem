namespace ECommerceSystem.OrderService.Saga;

/// <summary>
/// A saga step failed for a reason that may go away (Payment Service down, refund not possible yet).
/// RabbitMqConsumerBase treats every non-poison exception as transient: the message is retried after
/// 1 s / 2 s / 4 s, and dead-lettered to "order.inventory-results.dlq" when the retries are exhausted.
/// </summary>
public sealed class SagaTransientException(string message) : Exception(message);