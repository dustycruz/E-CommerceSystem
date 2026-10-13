using System.Net.Http.Json;
using System.Text.Json;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace ECommerceSystem.Storefront.Clients.Inventory;

/// <summary>Only the two fields the Storefront needs from StockItemResponse (contracts\inventory.yaml).</summary>
internal sealed record StockLevel(int ProductId, int QuantityAvailable);

/// <summary>
/// Stock levels are only a HINT (eventual consistency): the real check happens when the Inventory Service
/// reserves stock for an order. So this call has a short 3-second budget and never blocks the shop -
/// when Inventory is down the products are simply shown without stock hints.
/// </summary>
public sealed class InventoryClient(HttpClient httpClient, ILogger<InventoryClient> logger) : IInventoryClient
{
    private static readonly TimeSpan Budget = TimeSpan.FromSeconds(3);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyDictionary<int, int>?> GetAvailabilityAsync(CancellationToken cancellationToken)
    {
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        budget.CancelAfter(Budget);

        try
        {
            var items = await httpClient.GetFromJsonAsync<List<StockLevel>>("inventory/v1/stock-items", JsonOptions, budget.Token);
            return items?.ToDictionary(i => i.ProductId, i => i.QuantityAvailable);
        }
        catch (Exception ex) when (ex is HttpRequestException or TimeoutRejectedException or BrokenCircuitException or JsonException
                                   || (ex is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            logger.LogWarning("Stock levels unavailable ({Error}); products are shown without stock hints.", ex.Message);
            return null;
        }
    }
}