namespace ECommerceSystem.Shared.Events;

/// <summary>Explains why a line could not be reserved (carried by StockReservationFailed).</summary>
public sealed record StockShortage(int ProductId, string Sku, int Requested, int Available);