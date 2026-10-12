using System.ComponentModel.DataAnnotations;
using ECommerceSystem.PaymentService.Data.Entities;

namespace ECommerceSystem.PaymentService.Contracts;

/// <summary>Body of POST /payments/v1/payments - manual (admin) payment record (schema CreatePaymentRequest).</summary>
public sealed class CreatePaymentRequest : IValidatableObject
{
    [Required]
    public Guid OrderId { get; init; }

    [Required]
    [Range(typeof(decimal), "0.01", "10000000", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal Amount { get; init; }

    [Required]
    [RegularExpression("^PHP$", ErrorMessage = "Currency must be PHP.")]
    public string Currency { get; init; } = string.Empty;

    [Required]
    [RegularExpression("^[0-9]{4}$", ErrorMessage = "CardLast4 must be exactly 4 digits.")]
    public string CardLast4 { get; init; } = string.Empty;

    [Required]
    public PaymentStatus? Status { get; init; }

    [StringLength(500)]
    public string? FailureReason { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (OrderId == Guid.Empty)
        {
            yield return new ValidationResult("OrderId must be a non-empty GUID.", [nameof(OrderId)]);
        }
    }
}