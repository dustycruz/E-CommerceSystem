using ECommerceSystem.Storefront.Clients.Orders;
using ECommerceSystem.Storefront.Models;
using ECommerceSystem.Storefront.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceSystem.Storefront.Controllers;

/// <summary>
/// Checkout: validates the form, sends ONE POST /orders/v1/orders (never retried) and redirects to the live
/// order page on 202. Problem Details (400 / 422 / 503) are shown on the form; the cart is kept.
/// </summary>
public sealed class CheckoutController(
    CartService cart,
    CartSummaryBuilder cartSummary,
    IOrderApiClient orderApiClient,
    ILogger<CheckoutController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var summary = await cartSummary.BuildAsync(cancellationToken);
        if (summary.IsEmpty)
        {
            TempData["Error"] = "Your cart is empty. Add a product before checking out.";
            return RedirectToAction("Index", "Cart");
        }

        var customer = cart.GetCustomer();
        var form = new CheckoutForm
        {
            CustomerName = customer?.Name ?? string.Empty,
            CustomerEmail = customer?.Email ?? string.Empty
        };

        return View(new CheckoutViewModel { Form = form, Cart = summary });
    }

    [HttpPost]
    public async Task<IActionResult> Index([Bind(Prefix = "Form")] CheckoutForm form, CancellationToken cancellationToken)
    {
        var summary = await cartSummary.BuildAsync(cancellationToken);
        if (summary.IsEmpty)
        {
            TempData["Error"] = "Your cart is empty. Add a product before checking out.";
            return RedirectToAction("Index", "Cart");
        }

        if (!ModelState.IsValid)
        {
            return View(new CheckoutViewModel { Form = form, Cart = summary });
        }

        var command = new PlaceOrderCommand(
            form.CustomerName.Trim(),
            form.CustomerEmail.Trim(),
            form.CardNumber,
            form.SimulateFailureAfterPayment,
            cart.GetLines().Select(l => new PlaceOrderLine(l.ProductId, l.Quantity)).ToList());

        logger.LogInformation(
            "Checkout: placing order for {CustomerEmail} with {LineCount} line(s), simulate failure after payment = {Simulate}.",
            command.CustomerEmail,
            command.Items.Count,
            command.SimulateFailureAfterPayment);

        var result = await orderApiClient.PlaceOrderAsync(command, cancellationToken);

        if (result.IsSuccess)
        {
            var order = result.Value!;
            cart.Clear();
            cart.RememberCustomer(command.CustomerName, command.CustomerEmail);

            logger.LogInformation(
                "Checkout: order {OrderId} accepted as {Status}, total {TotalAmount} {Currency}.",
                order.Id,
                order.Status,
                order.TotalAmount,
                order.Currency);

            TempData["Success"] = "Order placed! We are reserving your stock and charging your card.";
            return RedirectToAction("Details", "Orders", new { id = order.Id });
        }

        var problem = result.Problem!;
        foreach (var (apiField, messages) in problem.Errors)
        {
            foreach (var message in messages)
            {
                ModelState.AddModelError(FormFieldFor(apiField), message);
            }
        }

        logger.LogWarning(
            "Checkout: order NOT placed - HTTP {Status} {Title}: {Detail}",
            problem.Status,
            problem.Title,
            problem.Detail);

        return View(new CheckoutViewModel { Form = form, Cart = summary, Problem = problem });
    }

    /// <summary>Maps Order Service field names to this form; item errors become form-level messages.</summary>
    private static string FormFieldFor(string apiField) => apiField.ToLowerInvariant() switch
    {
        "customername" => "Form.CustomerName",
        "customeremail" => "Form.CustomerEmail",
        "cardnumber" => "Form.CardNumber",
        _ => string.Empty
    };
}