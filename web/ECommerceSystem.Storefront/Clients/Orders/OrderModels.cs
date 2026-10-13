namespace ECommerceSystem.Storefront.Clients.Orders;

/// <summary>Status values of contracts\orders.yaml (OrderStatus enum, sent as strings).</summary>
public static class OrderStatuses
{
    public const string Pending = "Pending";
    public const string StockReserved = "StockReserved";
    public const string PaymentProcessing = "PaymentProcessing";
    public const string Confirmed = "Confirmed";
    public const string Cancelled = "Cancelled";
}

/// <summary>Schema PlaceOrderItemRequest. No price: the Order Service reads prices from the Catalog.</summary>
public sealed record PlaceOrderLine(int ProductId, int Quantity);

/// <summary>Schema PlaceOrderRequest.</summary>
public sealed record PlaceOrderCommand(
    string CustomerName,
    string CustomerEmail,
    string CardNumber,
    bool SimulateFailureAfterPayment,
    IReadOnlyList<PlaceOrderLine> Items);

/// <summary>Schema OrderItemResponse.</summary>
public sealed record OrderItemDto(
    int ProductId,
    string Sku,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);

/// <summary>Schema OrderStatusHistoryResponse (one saga timeline entry).</summary>
public sealed record OrderHistoryDto(
    string Status,
    string Description,
    DateTime OccurredAtUtc);

/// <summary>Schema OrderResponse.</summary>
public sealed record OrderDto(
    Guid Id,
    string CustomerName,
    string CustomerEmail,
    string Status,
    decimal TotalAmount,
    string Currency,
    string CardLast4,
    string? FailureReason,
    Guid? PaymentId,
    string CorrelationId,
    bool SimulateFailureAfterPayment,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<OrderItemDto> Items,
    IReadOnlyList<OrderHistoryDto> History);

/// <summary>Schema OrderSummaryResponse ("My orders" row).</summary>
public sealed record OrderSummaryDto(
    Guid Id,
    string CustomerName,
    string CustomerEmail,
    string Status,
    decimal TotalAmount,
    string Currency,
    int ItemCount,
    string? FailureReason,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);