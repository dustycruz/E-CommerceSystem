namespace ECommerceSystem.InventoryService.Contracts;

/// <summary>Stock item as returned by the API (schema StockItemResponse in contracts\inventory.yaml).
/// RowVersion is serialized as base64; send it back in PUT for optimistic concurrency.</summary>
public sealed record StockItemResponse(
    int Id,
    int ProductId,
    string Sku,
    int QuantityOnHand,
    int QuantityReserved,
    int QuantityAvailable,
    byte[] RowVersion,
    DateTime UpdatedAtUtc);