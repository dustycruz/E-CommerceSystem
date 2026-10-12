using Asp.Versioning;
using ECommerceSystem.PaymentService.Contracts;
using ECommerceSystem.PaymentService.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceSystem.PaymentService.Controllers;

/// <summary>
/// Charge and refund, used by the Order Service saga (tag "PaymentOperations" in contracts\payments.yaml).
/// Same route prefix as PaymentsController.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("payments/v{version:apiVersion}/payments")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
public sealed class PaymentOperationsController(IPaymentService paymentService) : ControllerBase
{
    /// <summary>
    /// Charge a card for an order (operationId chargePayment).
    /// 201 = new payment (Charged or Declined), 200 = idempotent replay, 409 = same order with a different amount.
    /// </summary>
    [HttpPost("charge", Name = "chargePayment")]
    [Consumes("application/json")]
    [ProducesResponseType<PaymentResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<PaymentResponse>(StatusCodes.Status201Created, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<PaymentResponse>> ChargePayment(
        [FromBody] ChargePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await paymentService.ChargeAsync(request, cancellationToken);

        return result.Created
            ? CreatedAtRoute("getPaymentById", new { id = result.Payment.Id, version = "1" }, result.Payment)
            : Ok(result.Payment);
    }

    /// <summary>Refund a charged payment, idempotently (operationId refundPayment).</summary>
    [HttpPost("{id:guid}/refund", Name = "refundPayment")]
    [Consumes("application/json")]
    [ProducesResponseType<PaymentResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<PaymentResponse>> RefundPayment(
        Guid id,
        [FromBody] RefundPaymentRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await paymentService.RefundAsync(id, request, cancellationToken));
    }
}