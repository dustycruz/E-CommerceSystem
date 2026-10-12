using ECommerceSystem.InventoryService.Contracts;

namespace ECommerceSystem.InventoryService.Services;

/// <summary>HTTP use cases. Errors are Shared exceptions (404 / 409) turned into Problem Details.</summary>
public interface IStockItemService
{
    Task<IReadOnlyList<StockItemResponse>> ListAsync(CancellationToken cancellationToken);

    Task<StockItemResponse> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<StockItemResponse> GetByProductIdAsync(int productId, CancellationToken cancellationToken);

    Task<StockItemResponse> CreateAsync(CreateStockItemRequest request, CancellationToken cancellationToken);

    Task<StockItemResponse> UpdateAsync(int id, UpdateStockItemRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<ReservationResponse>> ListReservationsAsync(Guid? orderId, CancellationToken cancellationToken);
}