using ECommerceSystem.Storefront.Clients.Catalog;
using ECommerceSystem.Storefront.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceSystem.Storefront.Controllers;

/// <summary>Session cart: view, add, change quantity, remove, empty, and "start over".</summary>
public sealed class CartController(CartService cart, CartSummaryBuilder cartSummary, ICatalogStore catalogStore) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(await cartSummary.BuildAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> Add(int productId, string? returnUrl, CancellationToken cancellationToken, int quantity = 1)
    {
        var lookup = await catalogStore.GetAsync(productId, cancellationToken);

        if (lookup.Product is null)
        {
            TempData["Error"] = lookup.NotFound
                ? "That product no longer exists in the catalog."
                : $"Could not add the product: {lookup.Error} Please try again.";
            return RedirectBack(returnUrl);
        }

        if (!lookup.Product.IsActive)
        {
            TempData["Error"] = $"{lookup.Product.Name} is not available for sale right now.";
            return RedirectBack(returnUrl);
        }

        var refusal = cart.Add(productId, quantity);
        if (refusal is not null)
        {
            TempData["Error"] = refusal;
        }
        else
        {
            TempData["Success"] = $"Added {quantity} \u00D7 {lookup.Product.Name} to your cart.";
        }

        return RedirectBack(returnUrl);
    }

    [HttpPost]
    public IActionResult Update(int productId, int quantity)
    {
        cart.SetQuantity(productId, quantity);
        TempData["Success"] = quantity <= 0 ? "Item removed from your cart." : "Cart updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult Remove(int productId)
    {
        cart.Remove(productId);
        TempData["Success"] = "Item removed from your cart.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult Clear()
    {
        cart.Clear();
        TempData["Success"] = "Your cart is now empty.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Sidebar "Start over": empties the cart and forgets the remembered name and email.</summary>
    [HttpPost]
    public IActionResult Reset()
    {
        cart.Clear();
        cart.ForgetCustomer();
        TempData["Success"] = "Started over: your cart is empty and your details were forgotten on this browser.";
        return RedirectToAction("Index", "Home");
    }

    private IActionResult RedirectBack(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction(nameof(Index));
}