using Microsoft.AspNetCore.Http;

namespace ECommerceSystem.Shared.Problems;

/// <summary>
/// Exceptions that the GlobalExceptionHandler turns into Problem Details with a specific status code.
/// Services throw them from their service layer; controllers stay free of error-mapping code.
/// </summary>
public abstract class ProblemException(int statusCode, string title, string detail, Exception? innerException = null)
    : Exception(detail, innerException)
{
    public int StatusCode { get; } = statusCode;

    public string Title { get; } = title;
}

/// <summary>404 - the resource does not exist.</summary>
public sealed class NotFoundException(string detail)
    : ProblemException(StatusCodes.Status404NotFound, "Resource not found", detail);

/// <summary>409 - the request conflicts with the current state (duplicate key, stale rowversion, wrong status).</summary>
public sealed class ConflictException(string detail, Exception? innerException = null)
    : ProblemException(StatusCodes.Status409Conflict, "Conflict with the current state", detail, innerException);

/// <summary>422 - well-formed request that cannot be processed (e.g. inactive product).</summary>
public sealed class UnprocessableEntityException(string detail)
    : ProblemException(StatusCodes.Status422UnprocessableEntity, "Request cannot be processed", detail);

/// <summary>503 - a downstream dependency is unavailable after all retries.</summary>
public sealed class ServiceUnavailableException(string detail, Exception? innerException = null)
    : ProblemException(StatusCodes.Status503ServiceUnavailable, "Service temporarily unavailable", detail, innerException);