using System.ComponentModel.DataAnnotations;

namespace ECommerceSystem.PaymentService.Contracts;

/// <summary>
/// Body of POST /payments/v1/payments/charge (schema ChargePaymentRequest).
/// OrderId is the idempotency key. CardNumber is used for the decision only and is NEVER stored or logged.
/// </summary>
public sealed class ChargePaymentRequest : IValidatableObject
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
    [RegularExpression("^[0-9]{16}$", ErrorMessage = "CardNumber must be exactly 16 digits.")]
    public string CardNumber { get; init; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (OrderId == Guid.Empty)
        {
            yield return new ValidationResult("OrderId must be a non-empty GUID.", [nameof(OrderId)]);
        }
    }
}