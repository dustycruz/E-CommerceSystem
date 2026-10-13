using System.ComponentModel.DataAnnotations;
using ECommerceSystem.Storefront.Clients.Catalog;
using ECommerceSystem.Storefront.Clients.Orders;
using ECommerceSystem.Storefront.Infrastructure;
using ECommerceSystem.Storefront.Services;

namespace ECommerceSystem.Storefront.Models;

// ------------------------------------------------------------------ shop

/// <summary>Product card: the product plus units available (null = Inventory did not answer).</summary>
public sealed record ProductCardViewModel(StoreProduct Product, int? Available);

public sealed record HomeIndexViewModel(
    IReadOnlyList<ProductCardViewModel> Products,
    IReadOnlyList<string> Categories,
    string? SelectedCategory,
    string? CatalogError,
    bool StockKnown);

public sealed record ProductDetailsViewModel(StoreProduct? Product, int? Available, string? Error);

// ------------------------------------------------------------------ cart

/// <summary>Cart line joined with live Catalog data (Product is null when the Catalog did not answer).</summary>
public sealed record CartLineViewModel(int ProductId, int Quantity, StoreProduct? Product)
{
    public decimal LineTotal => Product is null ? 0m : Product.Price * Quantity;
}

public sealed record CartViewModel(IReadOnlyList<CartLineViewModel> Lines, string? CatalogError)
{
    public bool IsEmpty => Lines.Count == 0;

    public int ItemCount => Lines.Sum(l => l.Quantity);

    public decimal Total => Lines.Sum(l => l.LineTotal);

    public bool HasUnavailableItems => Lines.Any(l => l.Product is { IsActive: false });
}

// ------------------------------------------------------------------ checkout

/// <summary>Checkout form. Rules match PlaceOrderRequest in contracts\orders.yaml.</summary>
public sealed class CheckoutForm
{
    [Required(ErrorMessage = "Please enter your full name.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "The name can be at most 100 characters.")]
    [Display(Name = "Full name")]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [StringLength(200, ErrorMessage = "The email address can be at most 200 characters.")]
    [Display(Name = "Email address")]
    public string CustomerEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter a card number.")]
    [RegularExpression("^[0-9]{16}$", ErrorMessage = "The card number must be exactly 16 digits, without spaces.")]
    [Display(Name = "Card number")]
    public string CardNumber { get; set; } = string.Empty;

    [Display(Name = "Simulate a failure after payment (demo)")]
    public bool SimulateFailureAfterPayment { get; set; }
}

public sealed class CheckoutViewModel
{
    public CheckoutForm Form { get; init; } = new();

    public CartViewModel Cart { get; init; } = new([], null);

    /// <summary>Problem Details from the Order Service (400 / 422 / 503 ...), shown as a friendly alert.</summary>
    public ApiProblem? Problem { get; init; }
}

// ------------------------------------------------------------------ orders

public sealed record OrdersIndexViewModel(
    string? Email,
    IReadOnlyList<OrderSummaryDto> Orders,
    ApiProblem? Problem,
    string? EmailError)
{
    public int InProgressCount => Orders.Count(o => !Display.IsFinal(o.Status));

    public int ConfirmedCount => Orders.Count(o => o.Status == OrderStatuses.Confirmed);

    public int CancelledCount => Orders.Count(o => o.Status == OrderStatuses.Cancelled);
}

public sealed record OrderDetailsViewModel(OrderDto? Order, ApiProblem? Problem, bool NotFound);

// ------------------------------------------------------------------ system

public sealed record StatusViewModel(IReadOnlyList<ServiceStatus> Services, DateTime CheckedAtUtc)
{
    public int UpCount => Services.Count(s => s.IsUp);
}