namespace ECommerceSystem.Shared.Events;

/// <summary>One product line carried by OrderPlaced and StockReserved.</summary>
public sealed record OrderLine(int ProductId, string Sku, int Quantity);