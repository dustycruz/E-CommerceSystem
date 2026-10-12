using System.ComponentModel.DataAnnotations;
using Asp.Versioning;
using ECommerceSystem.OrderService.Contracts;
using ECommerceSystem.OrderService.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceSystem.OrderService.Controllers;

/// <summary>/orders/v1/orders - implements contracts\orders.yaml (tag "Orders").</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("orders/v{version:apiVersion}/orders")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
public sealed class OrdersController(IOrderService orderService) : ControllerBase
{
    /// <summary>List orders, newest first; filter by customerEmail for "My orders" (operationId listOrders).</summary>
    [HttpGet(Name = "listOrders")]
    [ProducesResponseType<IReadOnlyList<OrderSummaryResponse>>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<IReadOnlyList<OrderSummaryResponse>>> ListOrders(
        [FromQuery][EmailAddress][StringLength(200)] string? customerEmail,
        CancellationToken cancellationToken)
    {
        return Ok(await orderService.ListAsync(customerEmail, cancellationToken));
    }

    /// <summary>
    /// Place an order and start the saga (operationId placeOrder).
    /// 202 = accepted as Pending; 400 = invalid / unknown product; 422 = inactive product; 503 = Catalog unavailable.
    /// </summary>
    [HttpPost(Name = "placeOrder")]
    [Consumes("application/json")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status202Accepted, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable, "application/problem+json")]
    public async Task<ActionResult<OrderResponse>> PlaceOrder(
        [FromBody] PlaceOrderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await orderService.PlaceOrderAsync(request, cancellationToken);

        if (result.ValidationErrors is not null)
        {
            foreach (var (key, messages) in result.ValidationErrors)
            {
                foreach (var message in messages)
                {
                    ModelState.AddModelError(key, message);
                }
            }

            return ValidationProblem(ModelState);
        }

        var order = result.Order!;
        return AcceptedAtRoute("getOrderById", new { id = order.Id, version = "1" }, order);
    }

    /// <summary>Get one order with items and timeline (operationId getOrderById).</summary>
    [HttpGet("{id:guid}", Name = "getOrderById")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<OrderResponse>> GetOrderById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await orderService.GetByIdAsync(id, cancellationToken));
    }

    /// <summary>Update customer details of a Pending order (operationId updateOrder).</summary>
    [HttpPut("{id:guid}", Name = "updateOrder")]
    [Consumes("application/json")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<OrderResponse>> UpdateOrder(
        Guid id,
        [FromBody] UpdateOrderRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await orderService.UpdateAsync(id, request, cancellationToken));
    }

    /// <summary>Delete a Confirmed or Cancelled order (operationId deleteOrder).</summary>
    [HttpDelete("{id:guid}", Name = "deleteOrder")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> DeleteOrder(Guid id, CancellationToken cancellationToken)
    {
        await orderService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}