using ECommerceSystem.OrderService.Contracts;

namespace ECommerceSystem.OrderService.Services;

/// <summary>
/// Outcome of placing an order: accepted (202) or field validation errors (400).
/// 422 (inactive product) and 503 (Catalog unavailable) are raised as Shared ProblemExceptions.
/// </summary>
public sealed record PlaceOrderResult(
    OrderResponse? Order,
    IReadOnlyDictionary<string, string[]>? ValidationErrors)
{
    public static PlaceOrderResult Accepted(OrderResponse order) => new(order, null);

    public static PlaceOrderResult Invalid(IReadOnlyDictionary<string, string[]> errors) => new(null, errors);
}