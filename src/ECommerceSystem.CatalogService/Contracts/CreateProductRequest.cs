using System.ComponentModel.DataAnnotations;

namespace ECommerceSystem.CatalogService.Contracts;

/// <summary>Body of POST /catalog/v1/products (schema CreateProductRequest).</summary>
public sealed class CreateProductRequest
{
    [Required]
    [StringLength(32, MinimumLength = 3)]
    [RegularExpression("^[A-Z0-9-]+$", ErrorMessage = "Sku may only contain upper-case letters, digits and '-'.")]
    public string Sku { get; init; } = string.Empty;

    [Required]
    [StringLength(120, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; init; }

    [Required]
    [StringLength(60, MinimumLength = 1)]
    public string Category { get; init; } = string.Empty;

    [Required]
    [Range(typeof(decimal), "0.01", "1000000", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal Price { get; init; }

    [Url]
    [StringLength(500)]
    public string? ImageUrl { get; init; }

    public bool IsActive { get; init; } = true;
}