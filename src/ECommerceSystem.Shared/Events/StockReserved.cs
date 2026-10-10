namespace ECommerceSystem.Shared.Events;

/// <summary>
/// Published by Inventory when ALL lines of an order were reserved in one local transaction.
/// Routing key: inventory.stock-reserved. Consumed by the Order Service saga (next step: payment).
/// </summary>
public sealed record StockReserved(
    Guid OrderId,
    IReadOnlyList<OrderLine> Lines) : IntegrationEvent;