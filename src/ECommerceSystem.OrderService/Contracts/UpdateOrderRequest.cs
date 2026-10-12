using System.ComponentModel.DataAnnotations;

namespace ECommerceSystem.OrderService.Contracts;

/// <summary>Body of PUT /orders/v1/orders/{id} (schema UpdateOrderRequest). Only allowed while Pending.</summary>
public sealed class UpdateOrderRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string CustomerName { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(200)]
    public string CustomerEmail { get; init; } = string.Empty;
}