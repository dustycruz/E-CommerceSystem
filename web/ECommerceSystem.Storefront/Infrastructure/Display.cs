using System.Globalization;
using ECommerceSystem.Storefront.Clients.Orders;

namespace ECommerceSystem.Storefront.Infrastructure;

/// <summary>A small coloured stock pill on product cards and the product page.</summary>
public sealed record StockHint(string Css, string Icon, string Text);

/// <summary>Formatting helpers shared by all views: money, local time, order ids and status badges.</summary>
public static class Display
{
    private const string PesoSign = "\u20B1";

    public static string Money(decimal amount) => PesoSign + amount.ToString("N2", CultureInfo.InvariantCulture);

    /// <summary>The APIs return UTC; pages show this PC's local time (Philippine time on the demo laptop).</summary>
    public static string LocalTime(DateTime utc) =>
        DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime().ToString("MMM d, yyyy h:mm:ss tt", CultureInfo.InvariantCulture);

    public static string LocalDate(DateTime utc) =>
        DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime().ToString("MMM d, yyyy h:mm tt", CultureInfo.InvariantCulture);

    /// <summary>
    /// Last 8 hex digits of the order id. Order ids are version-7 GUIDs: their FIRST digits are a timestamp
    /// shared by many orders, the last digits are random.
    /// </summary>
    public static string ShortId(Guid id) => id.ToString("N")[^8..].ToUpperInvariant();

    public static bool IsFinal(string status) => status is OrderStatuses.Confirmed or OrderStatuses.Cancelled;

    public static string StatusLabel(string status) => status switch
    {
        OrderStatuses.Pending => "Pending",
        OrderStatuses.StockReserved => "Stock reserved",
        OrderStatuses.PaymentProcessing => "Processing payment",
        OrderStatuses.Confirmed => "Confirmed",
        OrderStatuses.Cancelled => "Cancelled",
        _ => status
    };

    /// <summary>CSS modifier used by badges, the order hero and the timeline.</summary>
    public static string StatusCss(string status) => status switch
    {
        OrderStatuses.Pending => "status-pending",
        OrderStatuses.StockReserved or OrderStatuses.PaymentProcessing => "status-progress",
        OrderStatuses.Confirmed => "status-confirmed",
        OrderStatuses.Cancelled => "status-cancelled",
        _ => "status-neutral"
    };

    public static string StatusIcon(string status) => status switch
    {
        OrderStatuses.Pending => "hourglass",
        OrderStatuses.StockReserved => "package-check",
        OrderStatuses.PaymentProcessing => "credit-card",
        OrderStatuses.Confirmed => "circle-check",
        OrderStatuses.Cancelled => "circle-x",
        _ => "circle"
    };

    /// <summary>Stock pill text; null when the Inventory Service did not answer (no hint shown).</summary>
    public static StockHint? Stock(int? available) => available switch
    {
        null => null,
        <= 0 => new StockHint("stock-out", "circle-x", "Out of stock"),
        <= 5 => new StockHint("stock-low", "triangle-alert", $"Only {available} left"),
        _ => new StockHint("stock-ok", "circle-check", "In stock")
    };

    public static string Initials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => "G",
            1 => parts[0][..1].ToUpperInvariant(),
            _ => (parts[0][..1] + parts[^1][..1]).ToUpperInvariant()
        };
    }
}