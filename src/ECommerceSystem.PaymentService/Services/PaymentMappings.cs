using ECommerceSystem.PaymentService.Contracts;
using ECommerceSystem.PaymentService.Data.Entities;

namespace ECommerceSystem.PaymentService.Services;

/// <summary>Entity -> DTO mapping (the API never exposes EF entities).</summary>
public static class PaymentMappings
{
    public static PaymentResponse ToResponse(this Payment payment) => new(
        payment.Id,
        payment.OrderId,
        payment.Amount,
        payment.Currency,
        payment.CardLast4,
        payment.Status,
        payment.FailureReason,
        payment.CorrelationId,
        DateTime.SpecifyKind(payment.CreatedAtUtc, DateTimeKind.Utc),
        DateTime.SpecifyKind(payment.UpdatedAtUtc, DateTimeKind.Utc),
        payment.RefundedAtUtc is null ? null : DateTime.SpecifyKind(payment.RefundedAtUtc.Value, DateTimeKind.Utc));
}