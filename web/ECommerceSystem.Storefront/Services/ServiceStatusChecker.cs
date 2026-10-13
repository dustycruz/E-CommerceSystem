using System.Diagnostics;

namespace ECommerceSystem.Storefront.Services;

/// <summary>Result of pinging one microservice's root endpoint.</summary>
public sealed record ServiceStatus(string Name, string BaseUrl, bool IsUp, int? HttpStatus, long ElapsedMs, string? Error)
{
    public int Port => new Uri(BaseUrl).Port;

    public string SwaggerUrl => BaseUrl.TrimEnd('/') + "/swagger";
}

/// <summary>
/// Powers the "Service status" page used during demos: one quick GET (2 s timeout, no retries) to each
/// service's "/" endpoint, all four in parallel.
/// </summary>
public sealed class ServiceStatusChecker(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<ServiceStatusChecker> logger)
{
    public const string ClientName = "service-status";

    private static readonly (string Name, string SettingKey)[] Services =
    [
        ("Catalog Service", "Services:CatalogBaseUrl"),
        ("Order Service", "Services:OrderBaseUrl"),
        ("Inventory Service", "Services:InventoryBaseUrl"),
        ("Payment Service", "Services:PaymentBaseUrl")
    ];

    public async Task<IReadOnlyList<ServiceStatus>> CheckAllAsync(CancellationToken cancellationToken)
    {
        var checks = Services.Select(s => CheckAsync(s.Name, configuration[s.SettingKey] ?? string.Empty, cancellationToken));
        return await Task.WhenAll(checks);
    }

    private async Task<ServiceStatus> CheckAsync(string name, string baseUrl, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient(ClientName);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var response = await client.GetAsync(baseUrl, cancellationToken);
            var isUp = response.IsSuccessStatusCode;

            return new ServiceStatus(
                name,
                baseUrl,
                isUp,
                (int)response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                isUp ? null : $"Answered HTTP {(int)response.StatusCode}.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning("Status check: {Service} at {BaseUrl} is DOWN ({Error}).", name, baseUrl, ex.Message);
            return new ServiceStatus(name, baseUrl, false, null, stopwatch.ElapsedMilliseconds, "Connection refused - the service is not running.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Status check: {Service} at {BaseUrl} did not answer within 2 s.", name, baseUrl);
            return new ServiceStatus(name, baseUrl, false, null, stopwatch.ElapsedMilliseconds, "No answer within 2 seconds.");
        }
    }
}