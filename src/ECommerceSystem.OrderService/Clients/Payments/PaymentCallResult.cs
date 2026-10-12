namespace ECommerceSystem.OrderService.Clients.Payments;

/// <summary>Result of IPaymentClient calls - never an exception, never a raw HTTP status.</summary>
public sealed record PaymentCallResult(
    PaymentCallStatus Status,
    PaymentDto? Payment,
    string? Error)
{
    public bool IsCharged => Status == PaymentCallStatus.Charged;

    public static PaymentCallResult FromPayment(PaymentDto payment) => payment.Status switch
    {
        "Charged" => new PaymentCallResult(PaymentCallStatus.Charged, payment, null),
        "Declined" => new PaymentCallResult(PaymentCallStatus.Declined, payment, payment.FailureReason ?? "Payment declined."),
        "Refunded" => new PaymentCallResult(PaymentCallStatus.Refunded, payment, null),
        _ => new PaymentCallResult(PaymentCallStatus.Rejected, payment, $"Unknown payment status '{payment.Status}'.")
    };

    public static PaymentCallResult Failure(PaymentCallStatus status, string error) =>
        new(status, null, error);
}