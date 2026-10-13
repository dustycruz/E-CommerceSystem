namespace ECommerceSystem.Storefront.Clients.Orders;

/// <summary>Typed client for the Order Service (contracts\orders.yaml).</summary>
public interface IOrderApiClient
{
    /// <summary>POST /orders/v1/orders - 202 Accepted (Pending) or 400 / 422 / 503 Problem Details.</summary>
    Task<ApiResult<OrderDto>> PlaceOrderAsync(PlaceOrderCommand command, CancellationToken cancellationToken);

    /// <summary>GET /orders/v1/orders/{id} - order with items and saga timeline.</summary>
    Task<ApiResult<OrderDto>> GetOrderAsync(Guid orderId, CancellationToken cancellationToken);

    /// <summary>GET /orders/v1/orders?customerEmail= - "My orders", newest first.</summary>
    Task<ApiResult<IReadOnlyList<OrderSummaryDto>>> ListOrdersAsync(string customerEmail, CancellationToken cancellationToken);
}