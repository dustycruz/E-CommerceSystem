using ECommerceSystem.Shared.Events;

namespace ECommerceSystem.InventoryService.Services;

/// <summary>Result of trying to reserve an order: all lines reserved, or nothing reserved with the reasons.</summary>
public sealed record ReservationOutcome(
    bool IsReserved,
    IReadOnlyList<OrderLine> Lines,
    string Reason,
    IReadOnlyList<StockShortage> Shortages)
{
    public static ReservationOutcome Reserved(IReadOnlyList<OrderLine> lines) =>
        new(true, lines, string.Empty, []);

    public static ReservationOutcome Failed(string reason, IReadOnlyList<StockShortage> shortages) =>
        new(false, [], reason, shortages);
}