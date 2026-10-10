namespace ECommerceSystem.OrderService.Data.Entities;

/// <summary>
/// One step of the saga timeline (shown on the Storefront order page).
/// Several entries may share a status (e.g. PaymentProcessing: "charged", then "refund issued").
/// </summary>
public class OrderStatusHistory
{
    public int Id { get; set; }

    public Guid OrderId { get; set; }

    public OrderStatus Status { get; set; }

    public string Description { get; set; } = string.Empty;

    public DateTime OccurredAtUtc { get; set; }
}