using Asp.Versioning;
using ECommerceSystem.CatalogService.Chaos;
using ECommerceSystem.CatalogService.Contracts;
using ECommerceSystem.Shared.Problems;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceSystem.CatalogService.Controllers;

/// <summary>
/// /catalog/v1/chaos - Development-only fault injection for the retry demo (tag "Chaos").
/// In any other environment it answers 404 Problem Details.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("catalog/v{version:apiVersion}/chaos")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
public sealed class ChaosController(
    ChaosState chaosState,
    IHostEnvironment environment,
    ILogger<ChaosController> logger) : ControllerBase
{
    /// <summary>Make the next N product lookups return 503 (operationId configureChaos).</summary>
    [HttpPost(Name = "configureChaos")]
    [Consumes("application/json")]
    [ProducesResponseType<ChaosResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public ActionResult<ChaosResponse> ConfigureChaos([FromBody] ChaosRequest request)
    {
        if (!environment.IsDevelopment())
        {
            throw new NotFoundException("The chaos endpoint is only available in the Development environment.");
        }

        chaosState.SetFailNextRequests(request.FailNextRequests);

        var message = request.FailNextRequests == 0
            ? "Chaos mode is off. Product lookups behave normally."
            : $"The next {request.FailNextRequests} product lookup(s) will return 503.";

        logger.LogWarning("CHAOS configured: {Message}", message);

        return Ok(new ChaosResponse(request.FailNextRequests, message));
    }
}