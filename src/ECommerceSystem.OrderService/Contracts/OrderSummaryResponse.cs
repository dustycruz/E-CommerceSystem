using ECommerceSystem.OrderService.Data.Entities;

namespace ECommerceSystem.OrderService.Contracts;

/// <summary>Row of the "My orders" list (schema OrderSummaryResponse).</summary>
public sealed record OrderSummaryResponse(
    Guid Id,
    string CustomerName,
    string CustomerEmail,
    OrderStatus Status,
    decimal TotalAmount,
    string Currency,
    int ItemCount,
    string? FailureReason,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);