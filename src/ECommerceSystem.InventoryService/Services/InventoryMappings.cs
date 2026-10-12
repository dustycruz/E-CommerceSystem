using ECommerceSystem.InventoryService.Contracts;
using ECommerceSystem.InventoryService.Data.Entities;

namespace ECommerceSystem.InventoryService.Services;

/// <summary>Entity -> DTO mapping (the API never exposes EF entities).</summary>
public static class InventoryMappings
{
    public static StockItemResponse ToResponse(this StockItem item) => new(
        item.Id,
        item.ProductId,
        item.Sku,
        item.QuantityOnHand,
        item.QuantityReserved,
        item.QuantityAvailable,
        item.RowVersion,
        DateTime.SpecifyKind(item.UpdatedAtUtc, DateTimeKind.Utc));

    public static ReservationResponse ToResponse(this StockReservation reservation) => new(
        reservation.Id,
        reservation.OrderId,
        reservation.ProductId,
        reservation.Quantity,
        reservation.Status,
        DateTime.SpecifyKind(reservation.CreatedAtUtc, DateTimeKind.Utc),
        reservation.UpdatedAtUtc is null ? null : DateTime.SpecifyKind(reservation.UpdatedAtUtc.Value, DateTimeKind.Utc));
}