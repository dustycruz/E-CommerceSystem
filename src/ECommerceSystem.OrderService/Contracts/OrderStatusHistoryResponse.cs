using ECommerceSystem.OrderService.Data.Entities;

namespace ECommerceSystem.OrderService.Contracts;

/// <summary>One saga timeline entry (schema OrderStatusHistoryResponse).</summary>
public sealed record OrderStatusHistoryResponse(
    OrderStatus Status,
    string Description,
    DateTime OccurredAtUtc);