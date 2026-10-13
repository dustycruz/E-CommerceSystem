using System.ComponentModel.DataAnnotations;
using ECommerceSystem.Storefront.Clients.Orders;
using ECommerceSystem.Storefront.Models;
using ECommerceSystem.Storefront.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceSystem.Storefront.Controllers;

/// <summary>"My orders" (by email), the order page with the saga timeline, and its live-refresh partial.</summary>
public sealed class OrdersController(IOrderApiClient orderApiClient, CartService cart) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? email, CancellationToken cancellationToken)
    {
        var remembered = cart.GetCustomer();
        email = string.IsNullOrWhiteSpace(email) ? remembered?.Email : email.Trim();

        if (string.IsNullOrWhiteSpace(email))
        {
            return View(new OrdersIndexViewModel(null, [], null, null));
        }

        if (email.Length > 200 || !new EmailAddressAttribute().IsValid(email))
        {
            return View(new OrdersIndexViewModel(email, [], null, "Please enter a valid email address."));
        }

        var result = await orderApiClient.ListOrdersAsync(email, cancellationToken);

        if (result.IsSuccess)
        {
            var sameCustomer = remembered is not null && string.Equals(remembered.Email, email, StringComparison.OrdinalIgnoreCase);
            cart.RememberCustomer(sameCustomer ? remembered!.Name : string.Empty, email);
        }

        return View(new OrdersIndexViewModel(
            email,
            result.Value ?? Array.Empty<OrderSummaryDto>(),
            result.Problem,
            null));
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var result = await orderApiClient.GetOrderAsync(id, cancellationToken);

        if (result.IsSuccess)
        {
            return View(new OrderDetailsViewModel(result.Value, null, false));
        }

        var notFound = result.Problem!.Status == StatusCodes.Status404NotFound;
        if (notFound)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
        }

        return View(new OrderDetailsViewModel(null, notFound ? null : result.Problem, notFound));
    }

    /// <summary>Partial HTML polled every 2 s by site.js while the saga is still running.</summary>
    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Live(Guid id, CancellationToken cancellationToken)
    {
        var result = await orderApiClient.GetOrderAsync(id, cancellationToken);

        return result.IsSuccess
            ? PartialView("_OrderLive", result.Value)
            : StatusCode(result.Problem!.Status >= 400 ? result.Problem.Status : StatusCodes.Status502BadGateway);
    }
}