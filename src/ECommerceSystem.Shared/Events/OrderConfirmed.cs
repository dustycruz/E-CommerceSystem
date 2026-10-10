namespace ECommerceSystem.Shared.Events;

/// <summary>
/// Published by the Order Service when payment succeeded and the order is Confirmed.
/// Routing key: order.confirmed. Consumed by Inventory (commit the reservation) and Notification.
/// </summary>
public sealed record OrderConfirmed(
    Guid OrderId,
    Guid PaymentId,
    decimal TotalAmount,
    string CustomerEmail) : IntegrationEvent;