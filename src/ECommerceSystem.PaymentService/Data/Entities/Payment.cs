namespace ECommerceSystem.PaymentService.Data.Entities;

/// <summary>
/// A payment for one order. OrderId is UNIQUE and acts as the idempotency key:
/// charging the same order twice returns the existing payment instead of charging again.
/// Only the last 4 card digits are stored - never the full card number.
/// </summary>
public class Payment
{
    public Guid Id { get; set; }

    /// <summary>Order id from the Order Service (no foreign key - different database).</summary>
    public Guid OrderId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "PHP";

    public string CardLast4 { get; set; } = string.Empty;

    public PaymentStatus Status { get; set; }

    public string? FailureReason { get; set; }

    /// <summary>X-Correlation-ID of the request that created the payment (tracing).</summary>
    public string? CorrelationId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public DateTime? RefundedAtUtc { get; set; }

    /// <summary>SQL Server rowversion: two concurrent refunds cannot both succeed.</summary>
    public byte[] RowVersion { get; set; } = [];
}