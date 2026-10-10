namespace ECommerceSystem.PaymentService.Data.Entities;

/// <summary>
/// Charged  -> money was taken (test card 4242424242424242).
/// Declined -> the card was refused (test card 4000000000000002); no money taken.
/// Refunded -> a Charged payment was given back (saga compensation).
/// Stored as a string column.
/// </summary>
public enum PaymentStatus
{
    Charged = 0,
    Declined = 1,
    Refunded = 2
}