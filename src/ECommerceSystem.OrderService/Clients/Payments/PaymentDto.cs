namespace ECommerceSystem.OrderService.Clients.Payments;

/// <summary>The fields of the Payment Service's PaymentResponse that the Order Service uses.</summary>
public sealed record PaymentDto(
    Guid Id,
    Guid OrderId,
    decimal Amount,
    string Currency,
    string CardLast4,
    string Status,
    string? FailureReason,
    DateTime? RefundedAtUtc);