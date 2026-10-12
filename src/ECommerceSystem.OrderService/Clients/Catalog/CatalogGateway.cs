using ECommerceSystem.OrderService.Clients.Catalog.Generated;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace ECommerceSystem.OrderService.Clients.Catalog;

/// <summary>
/// Wraps the NSwag-generated client (contracts\catalog.yaml) and translates every downstream outcome:
///   200 + active    -> Found
///   200 + inactive  -> Inactive
///   404             -> NotFound
///   5xx / timeout / connection refused / circuit open (after the resilience pipeline gave up) -> Unavailable
/// Retries already happened inside the HttpClient pipeline (AddECommerceHttpPipeline) before this code runs.
/// </summary>
public sealed class CatalogGateway(ICatalogApiClient catalogApiClient, ILogger<CatalogGateway> logger) : ICatalogGateway
{
    public async Task<CatalogLookupResult> GetProductAsync(int productId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await catalogApiClient.GetProductByIdAsync(productId, cancellationToken);

            var product = new CatalogProduct(
                response.Id,
                response.Sku,
                response.Name,
                response.Price,
                response.Currency,
                response.IsActive);

            if (!product.IsActive)
            {
                logger.LogWarning("Catalog product {ProductId} ({Sku}) is inactive.", product.Id, product.Sku);
                return CatalogLookupResult.Inactive(product);
            }

            logger.LogInformation(
                "Catalog product {ProductId} ({Sku}) found: {Price} {Currency}.",
                product.Id,
                product.Sku,
                product.Price,
                product.Currency);

            return CatalogLookupResult.Found(product);
        }
        catch (CatalogApiException ex) when (ex.StatusCode == StatusCodes.Status404NotFound)
        {
            logger.LogWarning("Catalog says product {ProductId} does not exist (404).", productId);
            return CatalogLookupResult.NotFound(productId);
        }
        catch (CatalogApiException ex)
        {
            logger.LogError(
                "Catalog lookup for product {ProductId} failed with HTTP {StatusCode} after all retries.",
                productId,
                ex.StatusCode);

            return CatalogLookupResult.Unavailable(productId, $"Catalog Service answered HTTP {ex.StatusCode} after all retries.");
        }
        catch (TimeoutRejectedException ex)
        {
            logger.LogError(ex, "Catalog lookup for product {ProductId} timed out (total request timeout reached).", productId);
            return CatalogLookupResult.Unavailable(productId, "Catalog Service did not answer in time.");
        }
        catch (BrokenCircuitException ex)
        {
            logger.LogError(ex, "Catalog lookup for product {ProductId} rejected: circuit breaker is open.", productId);
            return CatalogLookupResult.Unavailable(productId, "Catalog Service is temporarily unavailable (circuit open).");
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Catalog lookup for product {ProductId} failed: {Error}", productId, ex.Message);
            return CatalogLookupResult.Unavailable(productId, "Catalog Service cannot be reached.");
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "Catalog lookup for product {ProductId} timed out.", productId);
            return CatalogLookupResult.Unavailable(productId, "Catalog Service did not answer in time.");
        }
    }
}