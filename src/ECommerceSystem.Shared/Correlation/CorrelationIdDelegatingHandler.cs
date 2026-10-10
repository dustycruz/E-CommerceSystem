namespace ECommerceSystem.Shared.Correlation;

/// <summary>
/// Adds the current X-Correlation-ID to every outgoing HttpClient request
/// (registered on every typed client through AddECommerceHttpPipeline()).
/// </summary>
public sealed class CorrelationIdDelegatingHandler(ICorrelationIdAccessor correlationIdAccessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var correlationId = correlationIdAccessor.CorrelationId;

        if (!string.IsNullOrWhiteSpace(correlationId) && !request.Headers.Contains(CorrelationConstants.HeaderName))
        {
            request.Headers.TryAddWithoutValidation(CorrelationConstants.HeaderName, correlationId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}