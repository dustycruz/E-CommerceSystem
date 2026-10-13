using System.Net.Http.Json;
using System.Text.Json;
using ECommerceSystem.Shared.Correlation;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace ECommerceSystem.Storefront.Clients.Orders;

/// <summary>
/// Hand-written typed HttpClient for the Order Service. The HttpClient comes from IHttpClientFactory with the
/// correlation handler and a resilience pipeline that never retries POST (see Program.cs).
/// Every answer becomes an ApiResult: 2xx -> value; 4xx/5xx -> parsed Problem Details; transport failure ->
/// a synthesized 503 problem carrying the current correlation id.
/// </summary>
public sealed class OrderApiClient(
    HttpClient httpClient,
    ICorrelationIdAccessor correlationIdAccessor,
    ILogger<OrderApiClient> logger) : IOrderApiClient
{
    private const string OrdersPath = "orders/v1/orders";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly IReadOnlyDictionary<string, string[]> NoErrors = new Dictionary<string, string[]>();

    public Task<ApiResult<OrderDto>> PlaceOrderAsync(PlaceOrderCommand command, CancellationToken cancellationToken) =>
        SendAsync<OrderDto>(
            "place the order",
            () => httpClient.PostAsJsonAsync(OrdersPath, command, JsonOptions, cancellationToken),
            cancellationToken);

    public Task<ApiResult<OrderDto>> GetOrderAsync(Guid orderId, CancellationToken cancellationToken) =>
        SendAsync<OrderDto>(
            "load the order",
            () => httpClient.GetAsync($"{OrdersPath}/{orderId}", cancellationToken),
            cancellationToken);

    public Task<ApiResult<IReadOnlyList<OrderSummaryDto>>> ListOrdersAsync(string customerEmail, CancellationToken cancellationToken) =>
        SendAsync<IReadOnlyList<OrderSummaryDto>>(
            "load your orders",
            () => httpClient.GetAsync($"{OrdersPath}?customerEmail={Uri.EscapeDataString(customerEmail)}", cancellationToken),
            cancellationToken);

    private async Task<ApiResult<T>> SendAsync<T>(
        string operation,
        Func<Task<HttpResponseMessage>> send,
        CancellationToken cancellationToken) where T : class
    {
        try
        {
            using var response = await send();

            if (response.IsSuccessStatusCode)
            {
                var value = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
                if (value is not null)
                {
                    return ApiResult<T>.Ok(value);
                }

                return ApiResult<T>.Fail(new ApiProblem(
                    StatusCodes.Status502BadGateway,
                    "Unexpected answer",
                    $"The Order Service returned an empty answer when trying to {operation}.",
                    NoErrors,
                    CorrelationIdOf(response)));
            }

            var problem = await ReadProblemAsync(response, cancellationToken);
            logger.LogWarning(
                "Order Service could not {Operation}: HTTP {Status} {Title} - {Detail}",
                operation,
                problem.Status,
                problem.Title,
                problem.Detail);

            return ApiResult<T>.Fail(problem);
        }
        catch (Exception ex) when (IsCallFailure(ex, cancellationToken))
        {
            logger.LogError(ex, "Order Service unreachable while trying to {Operation}.", operation);
            return ApiResult<T>.Fail(ApiProblem.Unreachable("Order Service", correlationIdAccessor.CorrelationId));
        }
    }

    private static async Task<ApiProblem> ReadProblemAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var status = (int)response.StatusCode;
        var title = response.ReasonPhrase ?? "Request failed";
        string? detail = null;
        var correlationId = CorrelationIdOf(response);
        var errors = new Dictionary<string, string[]>();

        try
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(body))
            {
                using var document = JsonDocument.Parse(body);
                var root = document.RootElement;

                if (root.ValueKind == JsonValueKind.Object)
                {
                    title = GetString(root, "title") ?? title;
                    detail = GetString(root, "detail");
                    correlationId = GetString(root, "correlationId") ?? correlationId;

                    if (root.TryGetProperty("errors", out var errorsElement) && errorsElement.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var field in errorsElement.EnumerateObject())
                        {
                            errors[field.Name] = field.Value.ValueKind == JsonValueKind.Array
                                ? field.Value.EnumerateArray().Select(e => e.ToString()).ToArray()
                                : [field.Value.ToString()];
                        }
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Not Problem Details: keep the status code and reason phrase.
        }

        return new ApiProblem(status, title, detail, errors, correlationId);
    }

    private static string? GetString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static string? CorrelationIdOf(HttpResponseMessage response) =>
        response.Headers.TryGetValues(CorrelationConstants.HeaderName, out var values) ? values.FirstOrDefault() : null;

    private static bool IsCallFailure(Exception exception, CancellationToken cancellationToken) => exception switch
    {
        HttpRequestException => true,
        TimeoutRejectedException => true,
        BrokenCircuitException => true,
        JsonException => true,
        TaskCanceledException => !cancellationToken.IsCancellationRequested,
        _ => false
    };
}