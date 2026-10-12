namespace ECommerceSystem.OrderService.Clients.Catalog;

/// <summary>Result of ICatalogGateway.GetProductAsync - never an exception, never a raw HTTP status.</summary>
public sealed record CatalogLookupResult(
    CatalogLookupStatus Status,
    int ProductId,
    CatalogProduct? Product,
    string? Error)
{
    public static CatalogLookupResult Found(CatalogProduct product) =>
        new(CatalogLookupStatus.Found, product.Id, product, null);

    public static CatalogLookupResult Inactive(CatalogProduct product) =>
        new(CatalogLookupStatus.Inactive, product.Id, product, $"Product {product.Id} ({product.Sku}) is not available for sale.");

    public static CatalogLookupResult NotFound(int productId) =>
        new(CatalogLookupStatus.NotFound, productId, null, $"Product {productId} does not exist in the catalog.");

    public static CatalogLookupResult Unavailable(int productId, string error) =>
        new(CatalogLookupStatus.Unavailable, productId, null, error);
}