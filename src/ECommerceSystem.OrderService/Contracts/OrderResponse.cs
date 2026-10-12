using ECommerceSystem.OrderService.Data.Entities;

namespace ECommerceSystem.OrderService.Contracts;

/// <summary>Order with items and saga timeline (schema OrderResponse).</summary>
public sealed record OrderResponse(
    Guid Id,
    string CustomerName,
    string CustomerEmail,
    OrderStatus Status,
    decimal TotalAmount,
    string Currency,
    string CardLast4,
    string? FailureReason,
    Guid? PaymentId,
    string CorrelationId,
    bool SimulateFailureAfterPayment,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<OrderItemResponse> Items,
    IReadOnlyList<OrderStatusHistoryResponse> History);