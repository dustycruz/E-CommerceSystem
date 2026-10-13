namespace ECommerceSystem.Storefront.Clients.Orders;

/// <summary>
/// An RFC 9457 Problem Details answer (or a synthesized one when the service cannot be reached),
/// reduced to what the pages show: status, title, detail, field errors and the correlation id.
/// </summary>
public sealed record ApiProblem(
    int Status,
    string Title,
    string? Detail,
    IReadOnlyDictionary<string, string[]> Errors,
    string? CorrelationId)
{
    public static ApiProblem Unreachable(string service, string? correlationId) => new(
        StatusCodes.Status503ServiceUnavailable,
        $"{service} is unavailable",
        $"The {service} cannot be reached right now. Please try again in a moment.",
        new Dictionary<string, string[]>(),
        correlationId);
}

/// <summary>Either a value (2xx) or a problem - pages never see exceptions or raw HTTP responses.</summary>
public sealed record ApiResult<T>(T? Value, ApiProblem? Problem) where T : class
{
    public bool IsSuccess => Problem is null && Value is not null;

    public static ApiResult<T> Ok(T value) => new(value, null);

    public static ApiResult<T> Fail(ApiProblem problem) => new(null, problem);
}