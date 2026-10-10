namespace ECommerceSystem.OrderService.Data.Entities;

/// <summary>
/// Saga states of an order (orchestrated by the Order Service).
/// Pending -> StockReserved -> PaymentProcessing -> Confirmed
/// Any failure ends in Cancelled (after compensation where needed).
/// Stored as a string column so the database is readable.
/// </summary>
public enum OrderStatus
{
    Pending = 0,
    StockReserved = 1,
    PaymentProcessing = 2,
    Confirmed = 3,
    Cancelled = 4
}