using ECommerceSystem.CatalogService.Contracts;

namespace ECommerceSystem.CatalogService.Services;

/// <summary>
/// Product use cases. Failures are reported with Shared exceptions
/// (NotFoundException -> 404, ConflictException -> 409) that become Problem Details.
/// </summary>
public interface IProductService
{
    Task<IReadOnlyList<ProductResponse>> ListAsync(string? category, bool includeInactive, CancellationToken cancellationToken);

    Task<ProductResponse> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken);

    Task<ProductResponse> UpdateAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(int id, CancellationToken cancellationToken);
}