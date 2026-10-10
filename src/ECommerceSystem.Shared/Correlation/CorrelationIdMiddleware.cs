using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ECommerceSystem.Shared.Correlation;

/// <summary>
/// First middleware of every service:
/// 1. reads X-Correlation-ID from the request, or creates a new one (the Storefront edge always creates one),
/// 2. stores it in the accessor and HttpContext.Items,
/// 3. returns it in the X-Correlation-ID response header,
/// 4. opens a logging scope so EVERY log line of this request contains "CorrelationId:&lt;id&gt;".
/// </summary>
public sealed class CorrelationIdMiddleware(
    RequestDelegate next,
    ICorrelationIdAccessor correlationIdAccessor,
    ILogger<CorrelationIdMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var incoming = context.Request.Headers[CorrelationConstants.HeaderName].ToString();
        var correlationId = CorrelationIdGenerator.IsValid(incoming)
            ? incoming
            : CorrelationIdGenerator.NewId();

        correlationIdAccessor.CorrelationId = correlationId;
        context.Items[CorrelationConstants.HttpContextItemKey] = correlationId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationConstants.HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (logger.BeginScope(CorrelationConstants.LogScopeFormat, correlationId))
        {
            await next(context);
        }
    }
}