using Asp.Versioning;
using ECommerceSystem.PaymentService.Contracts;
using ECommerceSystem.PaymentService.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceSystem.PaymentService.Controllers;

/// <summary>/payments/v1/payments - payment records CRUD (tag "Payments" in contracts\payments.yaml).</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("payments/v{version:apiVersion}/payments")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
public sealed class PaymentsController(IPaymentService paymentService) : ControllerBase
{
    /// <summary>List payments, newest first (operationId listPayments).</summary>
    [HttpGet(Name = "listPayments")]
    [ProducesResponseType<IReadOnlyList<PaymentResponse>>(StatusCodes.Status200OK, "application/json")]
    public async Task<ActionResult<IReadOnlyList<PaymentResponse>>> ListPayments(CancellationToken cancellationToken)
    {
        return Ok(await paymentService.ListAsync(cancellationToken));
    }

    /// <summary>Create a payment record manually (operationId createPayment).</summary>
    [HttpPost(Name = "createPayment")]
    [Consumes("application/json")]
    [ProducesResponseType<PaymentResponse>(StatusCodes.Status201Created, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<PaymentResponse>> CreatePayment(
        [FromBody] CreatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var created = await paymentService.CreateAsync(request, cancellationToken);
        return CreatedAtRoute("getPaymentById", new { id = created.Id, version = "1" }, created);
    }

    /// <summary>Get one payment (operationId getPaymentById).</summary>
    [HttpGet("{id:guid}", Name = "getPaymentById")]
    [ProducesResponseType<PaymentResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<PaymentResponse>> GetPaymentById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await paymentService.GetByIdAsync(id, cancellationToken));
    }

    /// <summary>Correct a Declined payment (operationId updatePayment).</summary>
    [HttpPut("{id:guid}", Name = "updatePayment")]
    [Consumes("application/json")]
    [ProducesResponseType<PaymentResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<PaymentResponse>> UpdatePayment(
        Guid id,
        [FromBody] UpdatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await paymentService.UpdateAsync(id, request, cancellationToken));
    }

    /// <summary>Delete a Declined or Refunded payment record (operationId deletePayment).</summary>
    [HttpDelete("{id:guid}", Name = "deletePayment")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> DeletePayment(Guid id, CancellationToken cancellationToken)
    {
        await paymentService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Get the payment of an order (operationId getPaymentByOrderId).</summary>
    [HttpGet("by-order/{orderId:guid}", Name = "getPaymentByOrderId")]
    [ProducesResponseType<PaymentResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<PaymentResponse>> GetPaymentByOrderId(Guid orderId, CancellationToken cancellationToken)
    {
        return Ok(await paymentService.GetByOrderIdAsync(orderId, cancellationToken));
    }
}