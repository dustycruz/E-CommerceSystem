using Asp.Versioning;
using ECommerceSystem.InventoryService.Contracts;
using ECommerceSystem.InventoryService.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceSystem.InventoryService.Controllers;

/// <summary>/inventory/v1/reservations - read-only view of saga reservations (tag "Reservations").</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("inventory/v{version:apiVersion}/reservations")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
public sealed class ReservationsController(IStockItemService stockItemService) : ControllerBase
{
    /// <summary>List reservations, newest first (operationId listReservations).</summary>
    [HttpGet(Name = "listReservations")]
    [ProducesResponseType<IReadOnlyList<ReservationResponse>>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<IReadOnlyList<ReservationResponse>>> ListReservations(
        [FromQuery] Guid? orderId,
        CancellationToken cancellationToken)
    {
        return Ok(await stockItemService.ListReservationsAsync(orderId, cancellationToken));
    }
}