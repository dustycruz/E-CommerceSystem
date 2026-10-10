using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ECommerceSystem.Shared.Problems;

/// <summary>
/// Turns every unhandled exception into an RFC 9457 Problem Details response (application/problem+json).
/// ProblemException subclasses keep their status code (404/409/422/503); anything else is a 500.
/// The correlationId and traceId members are added by the CustomizeProblemDetails callback.
/// </summary>
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    IHostEnvironment environment,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        int statusCode;
        string title;
        string detail;

        switch (exception)
        {
            case ProblemException problem:
                statusCode = problem.StatusCode;
                title = problem.Title;
                detail = problem.Message;
                break;

            case BadHttpRequestException badRequest:
                statusCode = StatusCodes.Status400BadRequest;
                title = "Bad request";
                detail = badRequest.Message;
                break;

            default:
                statusCode = StatusCodes.Status500InternalServerError;
                title = "An unexpected error occurred";
                detail = environment.IsDevelopment()
                    ? exception.Message
                    : "The error was logged. Quote the correlationId when reporting it.";
                break;
        }

        if (statusCode >= 500)
        {
            logger.LogError(exception, "Request {Method} {Path} failed with {StatusCode}: {Detail}",
                httpContext.Request.Method, httpContext.Request.Path, statusCode, detail);
        }
        else
        {
            logger.LogWarning("Request {Method} {Path} returned {StatusCode}: {Detail}",
                httpContext.Request.Method, httpContext.Request.Path, statusCode, detail);
        }

        if (statusCode == StatusCodes.Status503ServiceUnavailable)
        {
            httpContext.Response.Headers.RetryAfter = "5";
        }

        httpContext.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = ProblemTypes.ForStatus(statusCode),
            Instance = httpContext.Request.Path
        };

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });
    }
}