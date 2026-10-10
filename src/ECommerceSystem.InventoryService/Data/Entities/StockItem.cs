namespace ECommerceSystem.InventoryService.Data.Entities;

/// <summary>
/// Stock level for one catalog product. ProductId and Sku are copies of Catalog values
/// (no cross-database foreign key - database-per-service).
/// </summary>
public class StockItem
{
    public int Id { get; set; }

    /// <summary>Catalog product id. Unique: one stock row per product.</summary>
    public int ProductId { get; set; }

    public string Sku { get; set; } = string.Empty;

    /// <summary>Physical units in the warehouse (includes reserved units).</summary>
    public int QuantityOnHand { get; set; }

    /// <summary>Units held by in-flight orders that are not yet confirmed.</summary>
    public int QuantityReserved { get; set; }

    /// <summary>SQL Server rowversion used as an optimistic concurrency token (prevents overselling).</summary>
    public byte[] RowVersion { get; set; } = [];

    public DateTime UpdatedAtUtc { get; set; }

    /// <summary>Units that can still be reserved. Not stored in the database.</summary>
    public int QuantityAvailable => QuantityOnHand - QuantityReserved;
}