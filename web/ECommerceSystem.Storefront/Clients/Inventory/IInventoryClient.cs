namespace ECommerceSystem.Storefront.Clients.Inventory;

/// <summary>Stock levels for the "In stock / Only N left" hints on product cards.</summary>
public interface IInventoryClient
{
    /// <summary>ProductId -> units available now, or null when the Inventory Service does not answer quickly.</summary>
    Task<IReadOnlyDictionary<int, int>?> GetAvailabilityAsync(CancellationToken cancellationToken);
}