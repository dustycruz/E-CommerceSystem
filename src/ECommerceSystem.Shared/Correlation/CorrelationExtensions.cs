using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ECommerceSystem.Shared.Correlation;

public static class CorrelationExtensions
{
    /// <summary>Registers the correlation id accessor (singleton) and the outbound DelegatingHandler.</summary>
    public static IServiceCollection AddECommerceCorrelation(this IServiceCollection services)
    {
        services.TryAddSingleton<ICorrelationIdAccessor, CorrelationIdAccessor>();
        services.TryAddTransient<CorrelationIdDelegatingHandler>();
        return services;
    }

    /// <summary>
    /// Adds the correlation middleware. Call it FIRST in the pipeline (before UseExceptionHandler),
    /// so error responses and their log lines also carry the correlation id.
    /// </summary>
    public static IApplicationBuilder UseECommerceCorrelation(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }
}