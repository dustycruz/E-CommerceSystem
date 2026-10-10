namespace ECommerceSystem.Shared.Correlation;

/// <summary>
/// AsyncLocal-based accessor. The value flows with async/await into everything the request or message
/// handler calls - including IHttpClientFactory DelegatingHandlers, which run outside the request DI scope.
/// Registered as a singleton.
/// </summary>
public sealed class CorrelationIdAccessor : ICorrelationIdAccessor
{
    private static readonly AsyncLocal<string?> Current = new();

    public string? CorrelationId
    {
        get => Current.Value;
        set => Current.Value = value;
    }
}