using ECommerceSystem.Storefront.Clients.Catalog.Generated;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace ECommerceSystem.Storefront.Clients.Catalog;

/// <summary>
/// Wraps the NSwag-generated client (generated at build time from contracts\catalog.yaml) and turns every
/// outcome into something a page can show. Retries, timeouts and the circuit breaker already ran inside the
/// HttpClient pipeline (AddECommerceHttpPipeline) before an exception reaches this class.
/// </summary>
public sealed class CatalogStore(ICatalogApiClient catalogApiClient, ILogger<CatalogStore> logger) : ICatalogStore
{
    public async Task<CatalogListResult> ListAsync(bool includeInactive, CancellationToken cancellationToken)
    {
        try
        {
            var products = await catalogApiClient.ListProductsAsync(null, includeInactive, cancellationToken);
            return CatalogListResult.Ok(products.Select(Map).ToList());
        }
        catch (Exception ex) when (DescribeFailure(ex, cancellationToken) is { } error)
        {
            logger.LogError(ex, "Product list could not be loaded: {Error}", error);
            return CatalogListResult.Failed(error);
        }
    }

    public async Task<CatalogItemResult> GetAsync(int productId, CancellationToken cancellationToken)
    {
        try
        {
            var product = await catalogApiClient.GetProductByIdAsync(productId, cancellationToken);
            return CatalogItemResult.Found(Map(product));
        }
        catch (CatalogApiException ex) when (ex.StatusCode == StatusCodes.Status404NotFound)
        {
            logger.LogInformation("Catalog product {ProductId} does not exist (404).", productId);
            return CatalogItemResult.Missing();
        }
        catch (Exception ex) when (DescribeFailure(ex, cancellationToken) is { } error)
        {
            logger.LogError(ex, "Product {ProductId} could not be loaded: {Error}", productId, error);
            return CatalogItemResult.Failed(error);
        }
    }

    private static StoreProduct Map(ProductResponse product) => new(
        product.Id,
        product.Sku,
        product.Name,
        product.Description,
        product.Category,
        product.Price,
        product.Currency,
        product.ImageUrl,
        product.IsActive);

    private static string? DescribeFailure(Exception exception, CancellationToken cancellationToken) => exception switch
    {
        CatalogApiException api => $"The Catalog Service answered HTTP {api.StatusCode}.",
        TimeoutRejectedException => "The Catalog Service did not answer in time.",
        BrokenCircuitException => "The Catalog Service is temporarily unavailable (circuit open).",
        HttpRequestException => "The Catalog Service cannot be reached.",
        TaskCanceledException when !cancellationToken.IsCancellationRequested => "The Catalog Service did not answer in time.",
        _ => null
    };
}