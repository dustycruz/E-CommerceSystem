namespace ECommerceSystem.OrderService.Contracts;

/// <summary>Order line (schema OrderItemResponse). UnitPrice is the Catalog price at order time.</summary>
public sealed record OrderItemResponse(
    int ProductId,
    string Sku,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);