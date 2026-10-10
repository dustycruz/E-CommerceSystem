namespace ECommerceSystem.OrderService.Data.Entities;

/// <summary>
/// One order line. Sku, ProductName and UnitPrice are copied from the Catalog Service when the
/// order is placed, so later catalog price changes never alter an existing order.
/// </summary>
public class OrderItem
{
    public int Id { get; set; }

    public Guid OrderId { get; set; }

    public int ProductId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public decimal LineTotal { get; set; }
}