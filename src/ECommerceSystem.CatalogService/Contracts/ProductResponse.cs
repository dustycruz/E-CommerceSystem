namespace ECommerceSystem.CatalogService.Contracts;

/// <summary>Product as returned by the API (schema ProductResponse in contracts\catalog.yaml).</summary>
public sealed record ProductResponse(
    int Id,
    string Sku,
    string Name,
    string Description,
    string Category,
    decimal Price,
    string Currency,
    string ImageUrl,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);