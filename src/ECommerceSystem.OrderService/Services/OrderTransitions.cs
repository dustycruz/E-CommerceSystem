using ECommerceSystem.OrderService.Data.Entities;

namespace ECommerceSystem.OrderService.Services;

/// <summary>
/// Every status change is persisted in OrderStatusHistory, so the saga can always be inspected
/// (Storefront timeline, verify-orders.sql section 9).
/// </summary>
public static class OrderTransitions
{
    private const int MaxDescriptionLength = 500;

    /// <summary>Changes the status and appends a timeline entry.</summary>
    public static void TransitionTo(this Order order, OrderStatus newStatus, string description, DateTime nowUtc)
    {
        order.Status = newStatus;
        order.UpdatedAtUtc = nowUtc;
        order.AddHistory(newStatus, description, nowUtc);
    }

    /// <summary>Appends a timeline entry without changing the status (e.g. "refund issued").</summary>
    public static void AddHistory(this Order order, OrderStatus status, string description, DateTime nowUtc)
    {
        order.StatusHistory.Add(new OrderStatusHistory
        {
            OrderId = order.Id,
            Status = status,
            Description = Truncate(description),
            OccurredAtUtc = nowUtc
        });
    }

    public static string Truncate(string value) =>
        value.Length <= MaxDescriptionLength ? value : value[..MaxDescriptionLength];
}