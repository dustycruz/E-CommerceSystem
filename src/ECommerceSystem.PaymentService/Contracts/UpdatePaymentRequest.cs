using System.ComponentModel.DataAnnotations;

namespace ECommerceSystem.PaymentService.Contracts;

/// <summary>Body of PUT /payments/v1/payments/{id} - corrects a Declined payment (schema UpdatePaymentRequest).</summary>
public sealed class UpdatePaymentRequest
{
    [Required]
    [Range(typeof(decimal), "0.01", "10000000", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal Amount { get; init; }

    [Required]
    [RegularExpression("^[0-9]{4}$", ErrorMessage = "CardLast4 must be exactly 4 digits.")]
    public string CardLast4 { get; init; } = string.Empty;

    [StringLength(500)]
    public string? FailureReason { get; init; }
}