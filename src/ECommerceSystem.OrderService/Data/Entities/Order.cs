namespace ECommerceSystem.OrderService.Data.Entities;

/// <summary>
/// An order and the persisted state of its saga. Owned exclusively by the Order Service.
/// </summary>
public class Order
{
    public Guid Id { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string CustomerEmail { get; set; } = string.Empty;

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    /// <summary>Sum of all line totals, prices taken from the Catalog Service (decimal(18,2)).</summary>
    public decimal TotalAmount { get; set; }

    public string Currency { get; set; } = "PHP";

    /// <summary>Last 4 digits of the card, safe to display.</summary>
    public string CardLast4 { get; set; } = string.Empty;

    /// <summary>
    /// Card reference kept only until the charge attempt, then cleared (set to null).
    /// In production this is a payment-gateway token; here it holds a public TEST card number only.
    /// </summary>
    public string? PaymentToken { get; set; }

    /// <summary>Human-readable reason when the order is cancelled.</summary>
    public string? FailureReason { get; set; }

    /// <summary>Id of the payment in the Payment Service (no foreign key - different database).</summary>
    public Guid? PaymentId { get; set; }

    /// <summary>X-Correlation-ID generated at the Storefront edge, used to trace the whole saga.</summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>Demo switch: after a successful charge, fail and compensate (refund + release + cancel).</summary>
    public bool SimulateFailureAfterPayment { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    /// <summary>SQL Server rowversion: optimistic concurrency between API updates and saga steps.</summary>
    public byte[] RowVersion { get; set; } = [];

    public List<OrderItem> Items { get; set; } = [];

    public List<OrderStatusHistory> StatusHistory { get; set; } = [];
}