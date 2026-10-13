using ECommerceSystem.Storefront.Clients.Orders;

namespace ECommerceSystem.Storefront.Models;

/// <summary>One stage of the 4-stage progress bar on the order page. State: done, current, failed or todo.</summary>
public sealed record TrackerStep(string Label, string Icon, string State);

/// <summary>
/// Turns the order status + saga timeline into: Order placed -> Stock reserved -> Payment -> Confirmed/Cancelled.
/// A cancelled order shows WHERE the saga stopped: out of stock, payment failed, or payment refunded.
/// </summary>
public static class OrderTracker
{
    public static IReadOnlyList<TrackerStep> Build(OrderDto order)
    {
        var placed = new TrackerStep("Order placed", "receipt", "done");

        var reachedStock = order.History.Any(h =>
            h.Status is OrderStatuses.StockReserved or OrderStatuses.PaymentProcessing or OrderStatuses.Confirmed);

        var refunded = order.History.Any(h => h.Description.Contains("refunded", StringComparison.OrdinalIgnoreCase));

        return order.Status switch
        {
            OrderStatuses.Confirmed => new[]
            {
                placed,
                new TrackerStep("Stock reserved", "package-check", "done"),
                new TrackerStep("Payment", "credit-card", "done"),
                new TrackerStep("Confirmed", "circle-check", "done")
            },
            OrderStatuses.Cancelled when !reachedStock => new[]
            {
                placed,
                new TrackerStep("Out of stock", "package-x", "failed"),
                new TrackerStep("Payment", "credit-card", "todo"),
                new TrackerStep("Cancelled", "circle-x", "failed")
            },
            OrderStatuses.Cancelled => new[]
            {
                placed,
                new TrackerStep("Stock reserved", "package-check", "done"),
                new TrackerStep(refunded ? "Payment refunded" : "Payment failed", "credit-card", "failed"),
                new TrackerStep("Cancelled", "circle-x", "failed")
            },
            OrderStatuses.Pending => new[]
            {
                placed,
                new TrackerStep("Stock reserved", "package-check", "current"),
                new TrackerStep("Payment", "credit-card", "todo"),
                new TrackerStep("Confirmed", "circle-check", "todo")
            },
            _ => new[]
            {
                placed,
                new TrackerStep("Stock reserved", "package-check", "done"),
                new TrackerStep("Payment", "credit-card", "current"),
                new TrackerStep("Confirmed", "circle-check", "todo")
            }
        };
    }
}