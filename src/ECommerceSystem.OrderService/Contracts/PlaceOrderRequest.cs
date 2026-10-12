using System.ComponentModel.DataAnnotations;

namespace ECommerceSystem.OrderService.Contracts;

/// <summary>Body of POST /orders/v1/orders (schema PlaceOrderRequest).</summary>
public sealed class PlaceOrderRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string CustomerName { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(200)]
    public string CustomerEmail { get; init; } = string.Empty;

    /// <summary>Test card: 4242424242424242 succeeds, 4000000000000002 is declined.</summary>
    [Required]
    [RegularExpression("^[0-9]{16}$", ErrorMessage = "CardNumber must be exactly 16 digits.")]
    public string CardNumber { get; init; } = string.Empty;

    /// <summary>Demo switch: after a successful charge, refund + release stock + cancel.</summary>
    public bool SimulateFailureAfterPayment { get; init; }

    [Required]
    [MinLength(1)]
    [MaxLength(20)]
    public List<PlaceOrderItemRequest> Items { get; init; } = [];
}