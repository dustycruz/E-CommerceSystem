namespace ECommerceSystem.Storefront.Clients.Catalog;

/// <summary>A product as the Storefront shows it (mapped from the generated ProductResponse).</summary>
public sealed record StoreProduct(
    int Id,
    string Sku,
    string Name,
    string Description,
    string Category,
    decimal Price,
    string Currency,
    string ImageUrl,
    bool IsActive);

/// <summary>Product list, or a friendly error when the Catalog Service is unavailable.</summary>
public sealed record CatalogListResult(IReadOnlyList<StoreProduct> Products, string? Error)
{
    public bool IsSuccess => Error is null;

    public static CatalogListResult Ok(IReadOnlyList<StoreProduct> products) => new(products, null);

    public static CatalogListResult Failed(string error) => new([], error);
}

/// <summary>One product, "not found" (404), or a friendly error when the Catalog Service is unavailable.</summary>
public sealed record CatalogItemResult(StoreProduct? Product, bool NotFound, string? Error)
{
    public static CatalogItemResult Found(StoreProduct product) => new(product, false, null);

    public static CatalogItemResult Missing() => new(null, true, null);

    public static CatalogItemResult Failed(string error) => new(null, false, error);
}