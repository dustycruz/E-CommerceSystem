using ECommerceSystem.PaymentService.Data.Entities;

namespace ECommerceSystem.PaymentService.Services;

/// <summary>
/// Simulated card gateway with public TEST card numbers (on-demand demo triggers):
///   4242424242424242 -> Charged
///   4000000000000002 -> Declined (payment-declined compensation demo)
///   anything else    -> Declined (not a recognised test card)
/// </summary>
public static class TestCardGateway
{
    public const string SuccessCard = "4242424242424242";
    public const string DeclineCard = "4000000000000002";

    public static (PaymentStatus Status, string? FailureReason) Authorize(string cardNumber) => cardNumber switch
    {
        SuccessCard => (PaymentStatus.Charged, null),
        DeclineCard => (PaymentStatus.Declined, "Card declined by issuer (test card 4000000000000002)."),
        _ => (PaymentStatus.Declined, "Card not recognised by the test gateway. Use 4242424242424242 (success) or 4000000000000002 (decline).")
    };
}