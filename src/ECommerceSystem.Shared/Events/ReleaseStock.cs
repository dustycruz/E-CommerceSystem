namespace ECommerceSystem.Shared.Events;

/// <summary>
/// Compensation COMMAND sent by the Order Service when the saga fails after stock was reserved
/// (payment declined or failure after payment). Routing key: order.release-stock.
/// Consumed by Inventory, which releases the order's reservations idempotently.
/// </summary>
public sealed record ReleaseStock(
    Guid OrderId,
    string Reason) : IntegrationEvent;