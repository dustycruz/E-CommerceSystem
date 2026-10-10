namespace ECommerceSystem.Shared.Events;

/// <summary>
/// Published by the Order Service (through the transactional outbox) after the order is saved as Pending.
/// Routing key: order.placed. Consumed by Inventory (reserve all lines or none).
/// </summary>
public sealed record OrderPlaced(
    Guid OrderId,
    string CustomerEmail,
    decimal TotalAmount,
    string Currency,
    IReadOnlyList<OrderLine> Lines) : IntegrationEvent;