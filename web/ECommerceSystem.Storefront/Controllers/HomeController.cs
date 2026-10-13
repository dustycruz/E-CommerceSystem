using System.Diagnostics;
using ECommerceSystem.Shared.Correlation;
using ECommerceSystem.Storefront.Clients.Catalog;
using ECommerceSystem.Storefront.Clients.Inventory;
using ECommerceSystem.Storefront.Models;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceSystem.Storefront.Controllers;

/// <summary>Shop home: hero, category chips and the product grid (Catalog prices + Inventory stock hints).</summary>
public sealed class HomeController(ICatalogStore catalogStore, IInventoryClient inventoryClient) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? category, CancellationToken cancellationToken)
    {
        var catalog = await catalogStore.ListAsync(includeInactive: false, cancellationToken);
        var availability = catalog.IsSuccess ? await inventoryClient.GetAvailabilityAsync(cancellationToken) : null;

        var categories = catalog.Products
            .Select(p => p.Category)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c)
            .ToList();

        var selected = categories.FirstOrDefault(c => string.Equals(c, category, StringComparison.OrdinalIgnoreCase));

        var cards = catalog.Products
            .Where(p => selected is null || string.Equals(p.Category, selected, StringComparison.OrdinalIgnoreCase))
            .Select(p => new ProductCardViewModel(
                p,
                availability is not null && availability.TryGetValue(p.Id, out var units) ? units : (int?)null))
            .ToList();

        return View(new HomeIndexViewModel(cards, categories, selected, catalog.Error, availability is not null));
    }

    [IgnoreAntiforgeryToken]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            CorrelationId = HttpContext.Items[CorrelationConstants.HttpContextItemKey] as string
        });
    }
}