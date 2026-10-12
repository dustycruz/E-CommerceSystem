using ECommerceSystem.InventoryService.Data.Entities;

namespace ECommerceSystem.InventoryService.Contracts;

/// <summary>Stock reservation as returned by the API (schema ReservationResponse).</summary>
public sealed record ReservationResponse(
    int Id,
    Guid OrderId,
    int ProductId,
    int Quantity,
    ReservationStatus Status,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);