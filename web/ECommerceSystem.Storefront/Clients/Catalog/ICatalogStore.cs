namespace ECommerceSystem.Storefront.Clients.Catalog;

/// <summary>Read-only product access for the Storefront pages.</summary>
public interface ICatalogStore
{
    Task<CatalogListResult> ListAsync(bool includeInactive, CancellationToken cancellationToken);

    Task<CatalogItemResult> GetAsync(int productId, CancellationToken cancellationToken);
}