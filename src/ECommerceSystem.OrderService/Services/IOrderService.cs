using ECommerceSystem.OrderService.Contracts;

namespace ECommerceSystem.OrderService.Services;

/// <summary>Order use cases behind the HTTP API.</summary>
public interface IOrderService
{
    Task<PlaceOrderResult> PlaceOrderAsync(PlaceOrderRequest request, CancellationToken cancellationToken);

    Task<OrderResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<OrderSummaryResponse>> ListAsync(string? customerEmail, CancellationToken cancellationToken);

    Task<OrderResponse> UpdateAsync(Guid id, UpdateOrderRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}