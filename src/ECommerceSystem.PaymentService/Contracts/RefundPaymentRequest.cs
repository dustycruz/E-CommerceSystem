using System.ComponentModel.DataAnnotations;

namespace ECommerceSystem.PaymentService.Contracts;

/// <summary>Body of POST /payments/v1/payments/{id}/refund (schema RefundPaymentRequest).</summary>
public sealed class RefundPaymentRequest
{
    [StringLength(500)]
    public string? Reason { get; init; }
}