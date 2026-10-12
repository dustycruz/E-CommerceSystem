namespace ECommerceSystem.OrderService.Clients.Payments;

/// <summary>Typed client for the Payment Service (registered with IHttpClientFactory).</summary>
public interface IPaymentClient
{
    /// <summary>Charges the card for an order. Safe to retry: the Payment Service is idempotent by OrderId.</summary>
    Task<PaymentCallResult> ChargeAsync(PaymentChargeRequest request, CancellationToken cancellationToken);

    /// <summary>Refunds a payment (saga compensation). Safe to retry: refunds are idempotent.</summary>
    Task<PaymentCallResult> RefundAsync(Guid paymentId, string reason, CancellationToken cancellationToken);
}