using ECommerceSystem.CatalogService.Contracts;
using ECommerceSystem.CatalogService.Data.Entities;

namespace ECommerceSystem.CatalogService.Services;

/// <summary>Entity -> DTO mapping (the API never exposes EF entities).</summary>
public static class ProductMappings
{
    public const string Currency = "PHP";

    public static ProductResponse ToResponse(this Product product) => new(
        product.Id,
        product.Sku,
        product.Name,
        product.Description,
        product.Category,
        product.Price,
        Currency,
        product.ImageUrl,
        product.IsActive,
        DateTime.SpecifyKind(product.CreatedAtUtc, DateTimeKind.Utc),
        DateTime.SpecifyKind(product.UpdatedAtUtc, DateTimeKind.Utc));
}