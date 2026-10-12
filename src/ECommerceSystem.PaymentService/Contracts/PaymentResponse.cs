using ECommerceSystem.PaymentService.Data.Entities;

namespace ECommerceSystem.PaymentService.Contracts;

/// <summary>Payment as returned by the API (schema PaymentResponse in contracts\payments.yaml).</summary>
public sealed record PaymentResponse(
    Guid Id,
    Guid OrderId,
    decimal Amount,
    string Currency,
    string CardLast4,
    PaymentStatus Status,
    string? FailureReason,
    string? CorrelationId,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? RefundedAtUtc);