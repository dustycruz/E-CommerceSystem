namespace ECommerceSystem.OrderService.Clients.Payments;

/// <summary>Body sent to POST /payments/v1/payments/{id}/refund.</summary>
public sealed record PaymentRefundRequest(string? Reason);