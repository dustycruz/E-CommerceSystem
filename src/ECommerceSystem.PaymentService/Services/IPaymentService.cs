using ECommerceSystem.PaymentService.Contracts;

namespace ECommerceSystem.PaymentService.Services;

/// <summary>Payment use cases. Errors are Shared exceptions (404 / 409) turned into Problem Details.</summary>
public interface IPaymentService
{
    Task<IReadOnlyList<PaymentResponse>> ListAsync(CancellationToken cancellationToken);

    Task<PaymentResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PaymentResponse> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);

    Task<PaymentResponse> CreateAsync(CreatePaymentRequest request, CancellationToken cancellationToken);

    Task<PaymentResponse> UpdateAsync(Guid id, UpdatePaymentRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);

    Task<ChargeResult> ChargeAsync(ChargePaymentRequest request, CancellationToken cancellationToken);

    Task<PaymentResponse> RefundAsync(Guid id, RefundPaymentRequest request, CancellationToken cancellationToken);
}