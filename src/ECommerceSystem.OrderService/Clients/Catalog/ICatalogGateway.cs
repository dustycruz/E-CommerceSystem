namespace ECommerceSystem.OrderService.Clients.Catalog;

/// <summary>Anti-corruption layer over the NSwag-generated ICatalogApiClient.</summary>
public interface ICatalogGateway
{
    Task<CatalogLookupResult> GetProductAsync(int productId, CancellationToken cancellationToken);
}