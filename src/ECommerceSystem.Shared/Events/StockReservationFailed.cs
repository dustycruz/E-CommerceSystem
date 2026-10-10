namespace ECommerceSystem.Shared.Events;

/// <summary>
/// Published by Inventory when at least one line could not be reserved (nothing was reserved).
/// Routing key: inventory.stock-reservation-failed. Consumed by the Order Service saga (order cancelled).
/// </summary>
public sealed record StockReservationFailed(
    Guid OrderId,
    string Reason,
    IReadOnlyList<StockShortage> Shortages) : IntegrationEvent;