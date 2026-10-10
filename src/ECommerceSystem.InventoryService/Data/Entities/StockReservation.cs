namespace ECommerceSystem.InventoryService.Data.Entities;

/// <summary>
/// One reserved order line. All lines of an order are reserved in one local transaction (all or none).
/// </summary>
public class StockReservation
{
    public int Id { get; set; }

    /// <summary>Order id from the Order Service (Guid). Not a foreign key - different database.</summary>
    public Guid OrderId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public ReservationStatus Status { get; set; } = ReservationStatus.Reserved;

    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Set when the reservation is committed or released.</summary>
    public DateTime? UpdatedAtUtc { get; set; }
}