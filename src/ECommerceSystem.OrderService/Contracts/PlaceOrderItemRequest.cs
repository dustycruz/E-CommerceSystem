using System.ComponentModel.DataAnnotations;

namespace ECommerceSystem.OrderService.Contracts;

/// <summary>One requested line (schema PlaceOrderItemRequest). No price: prices come from the Catalog Service.</summary>
public sealed class PlaceOrderItemRequest
{
    [Required]
    [Range(1, int.MaxValue)]
    public int ProductId { get; init; }

    [Required]
    [Range(1, 100)]
    public int Quantity { get; init; }
}