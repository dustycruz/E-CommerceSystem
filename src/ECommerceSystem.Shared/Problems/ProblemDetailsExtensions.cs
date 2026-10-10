using System.Diagnostics;
using ECommerceSystem.Shared.Correlation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerceSystem.Shared.Problems;

public static class ProblemDetailsExtensions
{
    /// <summary>
    /// AddProblemDetails + global IExceptionHandler. Every Problem Details response (validation 400s from
    /// [ApiController], controller Problem()/NotFound() results, status-code pages and exceptions) gets:
    /// "correlationId", "traceId", a "type" URI and an "instance".
    /// </summary>
    public static IServiceCollection AddECommerceProblemDetails(this IServiceCollection services)
    {
        services.AddECommerceCorrelation();

        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                var httpContext = context.HttpContext;

                var correlationId =
                    httpContext.RequestServices.GetService<ICorrelationIdAccessor>()?.CorrelationId
                    ?? httpContext.Items[CorrelationConstants.HttpContextItemKey] as string
                    ?? httpContext.TraceIdentifier;

                context.ProblemDetails.Extensions["correlationId"] = correlationId;
                context.ProblemDetails.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;
                context.ProblemDetails.Instance ??= httpContext.Request.Path;
                context.ProblemDetails.Type ??= ProblemTypes.ForStatus(context.ProblemDetails.Status);
            };
        });

        services.AddExceptionHandler<GlobalExceptionHandler>();
        return services;
    }

    /// <summary>
    /// Exception handler (500/ProblemException -> Problem Details) and status-code pages
    /// (empty 404/405 responses -> Problem Details). Call right AFTER UseECommerceCorrelation().
    /// </summary>
    public static IApplicationBuilder UseECommerceProblemDetails(this IApplicationBuilder app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        return app;
    }
}