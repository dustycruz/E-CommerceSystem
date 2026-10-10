namespace ECommerceSystem.Shared.Events;

/// <summary>
/// Published by the Order Service when an order ends Cancelled (after any compensation).
/// Routing key: order.cancelled. Consumed by Notification (bonus).
/// </summary>
public sealed record OrderCancelled(
    Guid OrderId,
    string Reason,
    string CustomerEmail) : IntegrationEvent;