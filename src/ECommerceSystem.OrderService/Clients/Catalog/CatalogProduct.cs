namespace ECommerceSystem.OrderService.Clients.Catalog;

/// <summary>The Catalog data the Order Service needs (copied into OrderItem when the order is placed).</summary>
public sealed record CatalogProduct(
    int Id,
    string Sku,
    string Name,
    decimal Price,
    string Currency,
    bool IsActive);