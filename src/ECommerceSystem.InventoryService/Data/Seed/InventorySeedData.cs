using ECommerceSystem.InventoryService.Data.Entities;

namespace ECommerceSystem.InventoryService.Data.Seed;

/// <summary>
/// Constant seed data mirroring the Catalog products 1..8 (same ProductId and Sku values).
/// Product 8 (SKU-SPK-008) has only 2 units: ordering 3 or more triggers StockReservationFailed.
/// RowVersion is not set here: SQL Server generates it.
/// </summary>
public static class InventorySeedData
{
    private static readonly DateTime SeededAtUtc = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static StockItem[] StockItems { get; } =
    [
        new StockItem { Id = 1, ProductId = 1, Sku = "SKU-KBD-001", QuantityOnHand = 50, QuantityReserved = 0, UpdatedAtUtc = SeededAtUtc },
        new StockItem { Id = 2, ProductId = 2, Sku = "SKU-MSE-002", QuantityOnHand = 80, QuantityReserved = 0, UpdatedAtUtc = SeededAtUtc },
        new StockItem { Id = 3, ProductId = 3, Sku = "SKU-HDP-003", QuantityOnHand = 25, QuantityReserved = 0, UpdatedAtUtc = SeededAtUtc },
        new StockItem { Id = 4, ProductId = 4, Sku = "SKU-MON-004", QuantityOnHand = 10, QuantityReserved = 0, UpdatedAtUtc = SeededAtUtc },
        new StockItem { Id = 5, ProductId = 5, Sku = "SKU-CAM-005", QuantityOnHand = 40, QuantityReserved = 0, UpdatedAtUtc = SeededAtUtc },
        new StockItem { Id = 6, ProductId = 6, Sku = "SKU-SSD-006", QuantityOnHand = 60, QuantityReserved = 0, UpdatedAtUtc = SeededAtUtc },
        new StockItem { Id = 7, ProductId = 7, Sku = "SKU-CHR-007", QuantityOnHand = 15, QuantityReserved = 0, UpdatedAtUtc = SeededAtUtc },
        new StockItem { Id = 8, ProductId = 8, Sku = "SKU-SPK-008", QuantityOnHand = 2, QuantityReserved = 0, UpdatedAtUtc = SeededAtUtc }
    ];
}