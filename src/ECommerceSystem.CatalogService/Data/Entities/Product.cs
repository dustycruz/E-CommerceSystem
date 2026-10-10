namespace ECommerceSystem.CatalogService.Data.Entities;

/// <summary>
/// A product that can be sold in the storefront. Owned exclusively by the Catalog Service.
/// Prices are the single source of truth for the Order Service (it never trusts browser prices).
/// </summary>
public class Product
{
    public int Id { get; set; }

    /// <summary>Stock keeping unit, unique across the catalog, e.g. "SKU-KBD-001".</summary>
    public string Sku { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    /// <summary>Unit price in PHP, stored as decimal(18,2).</summary>
    public decimal Price { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>Inactive products are hidden from the storefront and rejected by the Order Service.</summary>
    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}