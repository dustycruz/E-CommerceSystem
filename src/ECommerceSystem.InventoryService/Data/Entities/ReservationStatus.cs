namespace ECommerceSystem.InventoryService.Data.Entities;

/// <summary>
/// Lifecycle of a stock reservation inside the saga.
/// Reserved  -> stock is held for an order (QuantityReserved increased).
/// Committed -> order confirmed; units leave QuantityOnHand and QuantityReserved.
/// Released  -> compensation; units go back to available (QuantityReserved decreased).
/// Stored as a string column so the database is readable.
/// </summary>
public enum ReservationStatus
{
    Reserved = 0,
    Released = 1,
    Committed = 2
}