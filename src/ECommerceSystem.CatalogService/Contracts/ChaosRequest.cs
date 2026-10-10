using System.ComponentModel.DataAnnotations;

namespace ECommerceSystem.CatalogService.Contracts;

/// <summary>Body of POST /catalog/v1/chaos (Development only).</summary>
public sealed class ChaosRequest
{
    [Required]
    [Range(0, 100)]
    public int FailNextRequests { get; init; }
}