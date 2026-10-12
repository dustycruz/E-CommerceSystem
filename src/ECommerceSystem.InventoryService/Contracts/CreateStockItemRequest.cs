using System.ComponentModel.DataAnnotations;

namespace ECommerceSystem.InventoryService.Contracts;

/// <summary>Body of POST /inventory/v1/stock-items (schema CreateStockItemRequest).</summary>
public sealed class CreateStockItemRequest
{
    [Required]
    [Range(1, int.MaxValue)]
    public int ProductId { get; init; }

    [Required]
    [StringLength(32, MinimumLength = 3)]
    [RegularExpression("^[A-Z0-9-]+$", ErrorMessage = "Sku may only contain upper-case letters, digits and '-'.")]
    public string Sku { get; init; } = string.Empty;

    [Required]
    [Range(0, 1000000)]
    public int QuantityOnHand { get; init; }
}