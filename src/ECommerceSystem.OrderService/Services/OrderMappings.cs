using ECommerceSystem.OrderService.Contracts;
using ECommerceSystem.OrderService.Data.Entities;

namespace ECommerceSystem.OrderService.Services;

/// <summary>Entity -> DTO mapping (the API never exposes EF entities). All times are returned as UTC.</summary>
public static class OrderMappings
{
    public static OrderResponse ToResponse(this Order order) => new(
        order.Id,
        order.CustomerName,
        order.CustomerEmail,
        order.Status,
        order.TotalAmount,
        order.Currency,
        order.CardLast4,
        order.FailureReason,
        order.PaymentId,
        order.CorrelationId,
        order.SimulateFailureAfterPayment,
        Utc(order.CreatedAtUtc),
        Utc(order.UpdatedAtUtc),
        order.Items
            .OrderBy(i => i.ProductId)
            .Select(i => new OrderItemResponse(i.ProductId, i.Sku, i.ProductName, i.UnitPrice, i.Quantity, i.LineTotal))
            .ToList(),
        order.StatusHistory
            .OrderBy(h => h.OccurredAtUtc)
            .ThenBy(h => h.Id)
            .Select(h => new OrderStatusHistoryResponse(h.Status, h.Description, Utc(h.OccurredAtUtc)))
            .ToList());

    public static DateTime Utc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
}