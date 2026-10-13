using ECommerceSystem.Storefront.Clients.Catalog;
using ECommerceSystem.Storefront.Clients.Inventory;
using ECommerceSystem.Storefront.Models;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceSystem.Storefront.Controllers;

/// <summary>Product page: /Products/Details/{id}.</summary>
public sealed class ProductsController(ICatalogStore catalogStore, IInventoryClient inventoryClient) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var result = await catalogStore.GetAsync(id, cancellationToken);

        if (result.Product is null)
        {
            if (result.NotFound)
            {
                Response.StatusCode = StatusCodes.Status404NotFound;
            }

            return View(new ProductDetailsViewModel(null, null, result.NotFound ? null : result.Error));
        }

        var availability = await inventoryClient.GetAvailabilityAsync(cancellationToken);
        int? available = availability is not null && availability.TryGetValue(id, out var units) ? units : null;

        return View(new ProductDetailsViewModel(result.Product, available, null));
    }
}