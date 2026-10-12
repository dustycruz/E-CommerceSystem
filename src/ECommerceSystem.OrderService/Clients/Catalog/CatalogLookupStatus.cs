namespace ECommerceSystem.OrderService.Clients.Catalog;

/// <summary>Domain outcome of looking up a product in the Catalog Service.</summary>
public enum CatalogLookupStatus
{
    /// <summary>The product exists and is active - it can be ordered.</summary>
    Found = 0,

    /// <summary>Catalog answered 404 - the product id does not exist.</summary>
    NotFound = 1,

    /// <summary>The product exists but IsActive = false - it cannot be ordered.</summary>
    Inactive = 2,

    /// <summary>Catalog could not be reached or kept failing after all retries (5xx, timeout, circuit open).</summary>
    Unavailable = 3
}