using System.Net.Http.Json;
using System.Text.Json;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace ECommerceSystem.OrderService.Clients.Payments;

/// <summary>
/// Hand-written typed HttpClient for the Payment Service. The HttpClient comes from IHttpClientFactory with
/// the same pipeline as the Catalog client (correlation header + retry/jitter + timeouts + circuit breaker).
/// Translates every outcome to PaymentCallResult. The card number is never logged.
/// </summary>
public sealed class PaymentClient(HttpClient httpClient, ILogger<PaymentClient> logger) : IPaymentClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<PaymentCallResult> ChargeAsync(PaymentChargeRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Calling Payment Service: charge {Amount} {Currency} for order {OrderId} (card ****{CardLast4}).",
            request.Amount,
            request.Currency,
            request.OrderId,
            request.CardNumber.Length >= 4 ? request.CardNumber[^4..] : "????");

        try
        {
            using var response = await httpClient.PostAsJsonAsync(
                "payments/v1/payments/charge",
                request,
                JsonOptions,
                cancellationToken);

            return await MapResponseAsync(response, $"charge for order {request.OrderId}", cancellationToken);
        }
        catch (Exception ex) when (IsTransportFailure(ex, cancellationToken))
        {
            logger.LogError(ex, "Payment Service unreachable while charging order {OrderId}.", request.OrderId);
            return PaymentCallResult.Failure(PaymentCallStatus.Unavailable, "Payment Service cannot be reached.");
        }
    }

    public async Task<PaymentCallResult> RefundAsync(Guid paymentId, string reason, CancellationToken cancellationToken)
    {
        logger.LogInformation("Calling Payment Service: refund payment {PaymentId}. Reason: {Reason}", paymentId, reason);

        try
        {
            using var response = await httpClient.PostAsJsonAsync(
                $"payments/v1/payments/{paymentId}/refund",
                new PaymentRefundRequest(reason),
                JsonOptions,
                cancellationToken);

            return await MapResponseAsync(response, $"refund of payment {paymentId}", cancellationToken);
        }
        catch (Exception ex) when (IsTransportFailure(ex, cancellationToken))
        {
            logger.LogError(ex, "Payment Service unreachable while refunding payment {PaymentId}.", paymentId);
            return PaymentCallResult.Failure(PaymentCallStatus.Unavailable, "Payment Service cannot be reached.");
        }
    }

    private async Task<PaymentCallResult> MapResponseAsync(
        HttpResponseMessage response,
        string operation,
        CancellationToken cancellationToken)
    {
        var statusCode = (int)response.StatusCode;

        if (response.IsSuccessStatusCode)
        {
            var payment = await response.Content.ReadFromJsonAsync<PaymentDto>(JsonOptions, cancellationToken);
            if (payment is null)
            {
                return PaymentCallResult.Failure(PaymentCallStatus.Unavailable, $"Payment Service returned an empty body for the {operation}.");
            }

            var result = PaymentCallResult.FromPayment(payment);
            logger.LogInformation(
                "Payment Service {Operation}: HTTP {StatusCode}, payment {PaymentId} is {PaymentStatus}.",
                operation,
                statusCode,
                payment.Id,
                payment.Status);

            return result;
        }

        var detail = await ReadProblemDetailAsync(response, cancellationToken);

        if (statusCode == StatusCodes.Status404NotFound)
        {
            logger.LogWarning("Payment Service {Operation}: 404 - {Detail}", operation, detail);
            return PaymentCallResult.Failure(PaymentCallStatus.NotFound, detail);
        }

        if (statusCode is StatusCodes.Status400BadRequest or StatusCodes.Status409Conflict)
        {
            logger.LogWarning("Payment Service {Operation}: HTTP {StatusCode} - {Detail}", operation, statusCode, detail);
            return PaymentCallResult.Failure(PaymentCallStatus.Rejected, detail);
        }

        logger.LogError("Payment Service {Operation}: HTTP {StatusCode} after all retries - {Detail}", operation, statusCode, detail);
        return PaymentCallResult.Failure(PaymentCallStatus.Unavailable, $"Payment Service answered HTTP {statusCode} after all retries.");
    }

    private static async Task<string> ReadProblemDetailAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(body);

            if (document.RootElement.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String)
            {
                return detail.GetString() ?? response.ReasonPhrase ?? "Unknown error";
            }

            if (document.RootElement.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
            {
                return title.GetString() ?? response.ReasonPhrase ?? "Unknown error";
            }
        }
        catch (JsonException)
        {
            // Not Problem Details - fall back to the reason phrase.
        }

        return response.ReasonPhrase ?? "Unknown error";
    }

    private static bool IsTransportFailure(Exception ex, CancellationToken cancellationToken) => ex switch
    {
        HttpRequestException => true,
        TimeoutRejectedException => true,
        BrokenCircuitException => true,
        TaskCanceledException => !cancellationToken.IsCancellationRequested,
        _ => false
    };
}