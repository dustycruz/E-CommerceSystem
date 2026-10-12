namespace ECommerceSystem.OrderService.Clients.Payments;

/// <summary>Body sent to POST /payments/v1/payments/charge (OrderId = idempotency key).</summary>
public sealed record PaymentChargeRequest(
    Guid OrderId,
    decimal Amount,
    string Currency,
    string CardNumber);