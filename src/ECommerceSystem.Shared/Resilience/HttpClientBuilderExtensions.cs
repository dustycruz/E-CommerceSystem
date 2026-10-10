using ECommerceSystem.Shared.Correlation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Polly;

namespace ECommerceSystem.Shared.Resilience;

/// <summary>
/// One call that gives a typed HttpClient (from IHttpClientFactory - never "new HttpClient()"):
///  1. CorrelationIdDelegatingHandler (outermost) - adds X-Correlation-ID to every request,
///  2. the standard resilience pipeline:
///     total timeout 10 s -> retry 3x (exponential back-off + jitter; 5xx/408/429/network errors) ->
///     circuit breaker (sampling 10 s, at least 2x the attempt timeout) -> attempt timeout 2 s.
///  Every retry, attempt timeout and circuit change is logged with the correlation id.
/// </summary>
public static class HttpClientBuilderExtensions
{
    public const int MaxRetryAttempts = 3;

    public static readonly TimeSpan AttemptTimeout = TimeSpan.FromSeconds(2);

    public static readonly TimeSpan TotalRequestTimeout = TimeSpan.FromSeconds(10);

    public static readonly TimeSpan CircuitBreakerSamplingDuration = TimeSpan.FromSeconds(10);

    public static IHttpClientBuilder AddECommerceHttpPipeline(this IHttpClientBuilder builder)
    {
        builder.Services.AddECommerceCorrelation();

        var clientName = builder.Name;

        builder.AddHttpMessageHandler<CorrelationIdDelegatingHandler>();

        builder.AddStandardResilienceHandler()
            .Configure((options, serviceProvider) =>
            {
                var logger = serviceProvider.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("ECommerceSystem.Shared.Resilience.HttpResilience");
                var correlationIdAccessor = serviceProvider.GetRequiredService<ICorrelationIdAccessor>();

                // Retry: 3 retries after the first attempt, exponential (about 0.5 s, 1 s, 2 s) with jitter.
                options.Retry.MaxRetryAttempts = MaxRetryAttempts;
                options.Retry.BackoffType = DelayBackoffType.Exponential;
                options.Retry.UseJitter = true;
                options.Retry.Delay = TimeSpan.FromMilliseconds(500);
                options.Retry.OnRetry = args =>
                {
                    var reason = args.Outcome.Exception is not null
                        ? $"{args.Outcome.Exception.GetType().Name}: {args.Outcome.Exception.Message}"
                        : $"HTTP {(int)args.Outcome.Result!.StatusCode} {args.Outcome.Result.ReasonPhrase}";

                    logger.LogWarning(
                        "HTTP retry {RetryNumber} of {MaxRetries} for client {ClientName} in {DelayMs} ms. Reason: {Reason}. CorrelationId {CorrelationId}",
                        args.AttemptNumber + 1,
                        MaxRetryAttempts,
                        clientName,
                        (int)args.RetryDelay.TotalMilliseconds,
                        reason,
                        correlationIdAccessor.CorrelationId ?? "(none)");

                    return default;
                };

                // Per-attempt timeout.
                options.AttemptTimeout.Timeout = AttemptTimeout;
                options.AttemptTimeout.OnTimeout = args =>
                {
                    logger.LogWarning(
                        "HTTP attempt for client {ClientName} timed out after {TimeoutMs} ms. CorrelationId {CorrelationId}",
                        clientName,
                        (int)args.Timeout.TotalMilliseconds,
                        correlationIdAccessor.CorrelationId ?? "(none)");

                    return default;
                };

                // Total timeout for the whole call including all retries.
                options.TotalRequestTimeout.Timeout = TotalRequestTimeout;

                // Circuit breaker: sampling duration must be at least 2x the attempt timeout (options validation).
                options.CircuitBreaker.SamplingDuration = CircuitBreakerSamplingDuration;
                options.CircuitBreaker.FailureRatio = 0.5;
                options.CircuitBreaker.MinimumThroughput = 20;
                options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
                options.CircuitBreaker.OnOpened = args =>
                {
                    logger.LogError(
                        "Circuit OPENED for client {ClientName} for {BreakSeconds} s - calls fail fast. CorrelationId {CorrelationId}",
                        clientName,
                        args.BreakDuration.TotalSeconds,
                        correlationIdAccessor.CorrelationId ?? "(none)");

                    return default;
                };
                options.CircuitBreaker.OnClosed = args =>
                {
                    logger.LogInformation("Circuit CLOSED for client {ClientName} - calls flow again.", clientName);
                    return default;
                };
            });

        return builder;
    }
}