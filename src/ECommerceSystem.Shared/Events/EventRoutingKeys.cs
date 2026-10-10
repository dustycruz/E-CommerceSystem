namespace ECommerceSystem.Shared.Events;

/// <summary>
/// Routing keys on the "ecommerce.events" topic exchange.
/// </summary>
public static class EventRoutingKeys
{
    /// <summary>Order Service -> Inventory (reserve stock), Notification.</summary>
    public const string OrderPlaced = "order.placed";

    /// <summary>Inventory -> Order Service (saga continues with payment), Notification.</summary>
    public const string StockReserved = "inventory.stock-reserved";

    /// <summary>Inventory -> Order Service (order cancelled, nothing to compensate), Notification.</summary>
    public const string StockReservationFailed = "inventory.stock-reservation-failed";

    /// <summary>Compensation COMMAND: Order Service -> Inventory (release the reservation), Notification.</summary>
    public const string ReleaseStock = "order.release-stock";

    /// <summary>Order Service -> Inventory (commit the reservation), Notification.</summary>
    public const string OrderConfirmed = "order.confirmed";

    /// <summary>Order Service -> Notification.</summary>
    public const string OrderCancelled = "order.cancelled";
}