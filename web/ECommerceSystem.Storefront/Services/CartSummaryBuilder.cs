using ECommerceSystem.Storefront.Clients.Catalog;
using ECommerceSystem.Storefront.Models;

namespace ECommerceSystem.Storefront.Services;

/// <summary>
/// Joins the session cart with LIVE Catalog data (one list call, inactive products included so the cart
/// can say "not available for sale"). When the Catalog is down the lines are still shown, without prices.
/// </summary>
public sealed class CartSummaryBuilder(CartService cart, ICatalogStore catalogStore)
{
    public async Task<CartViewModel> BuildAsync(CancellationToken cancellationToken)
    {
        var lines = cart.GetLines();
        if (lines.Count == 0)
        {
            return new CartViewModel([], null);
        }

        var catalog = await catalogStore.ListAsync(includeInactive: true, cancellationToken);
        var productsById = catalog.Products.ToDictionary(p => p.Id);

        var viewLines = lines
            .Select(l => new CartLineViewModel(l.ProductId, l.Quantity, productsById.GetValueOrDefault(l.ProductId)))
            .ToList();

        return new CartViewModel(viewLines, catalog.Error);
    }
}