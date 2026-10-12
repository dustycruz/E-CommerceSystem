namespace ECommerceSystem.OrderService.Clients.Payments;

/// <summary>Domain outcome of a call to the Payment Service.</summary>
public enum PaymentCallStatus
{
    /// <summary>Money was taken (new charge or idempotent replay of a successful charge).</summary>
    Charged = 0,

    /// <summary>The card was declined - a business result, not an error.</summary>
    Declined = 1,

    /// <summary>The payment is refunded (new refund or idempotent replay).</summary>
    Refunded = 2,

    /// <summary>404 - the payment does not exist.</summary>
    NotFound = 3,

    /// <summary>400/409 - the request was rejected (e.g. different amount for the same order).</summary>
    Rejected = 4,

    /// <summary>Payment Service could not be reached or kept failing after all retries.</summary>
    Unavailable = 5
}