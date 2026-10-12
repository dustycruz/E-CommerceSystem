using System.ComponentModel.DataAnnotations;

namespace ECommerceSystem.InventoryService.Contracts;

/// <summary>Body of PUT /inventory/v1/stock-items/{id} (schema UpdateStockItemRequest).</summary>
public sealed class UpdateStockItemRequest
{
    [Required]
    [StringLength(32, MinimumLength = 3)]
    [RegularExpression("^[A-Z0-9-]+$", ErrorMessage = "Sku may only contain upper-case letters, digits and '-'.")]
    public string Sku { get; init; } = string.Empty;

    [Required]
    [Range(0, 1000000)]
    public int QuantityOnHand { get; init; }

    /// <summary>Optional base64 rowVersion from a previous GET. Stale value -> 409.</summary>
    public byte[]? RowVersion { get; init; }
}