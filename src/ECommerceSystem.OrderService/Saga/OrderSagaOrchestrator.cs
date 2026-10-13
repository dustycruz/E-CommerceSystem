using ECommerceSystem.OrderService.Clients.Payments;
using ECommerceSystem.OrderService.Data;
using ECommerceSystem.OrderService.Data.Entities;
using ECommerceSystem.OrderService.Outbox;
using ECommerceSystem.OrderService.Services;
using ECommerceSystem.Shared.Events;
using ECommerceSystem.Shared.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ECommerceSystem.OrderService.Saga;

/// <summary>
/// Orchestrated saga - the Order Service decides every next step:
///   StockReservationFailed            -> Cancelled + OrderCancelled (nothing was reserved, nothing to compensate)
///   StockReserved                     -> PaymentProcessing, charge the card over HTTP (Payment Service)
///     Charged                         -> Confirmed + OrderConfirmed (Inventory commits the reservation)
///     Declined / rejected             -> ReleaseStock + Cancelled + OrderCancelled
///     Charged + simulated failure     -> refund -> ReleaseStock + Cancelled + OrderCancelled
///     Payment Service unavailable     -> transient: retried 1 s / 2 s / 4 s; the last attempt compensates
/// Every outgoing event is written to the outbox in the SAME SaveChanges as the status change, the
/// OrderStatusHistory entries and the ProcessedMessages row (idempotent consumer).
/// </summary>
public sealed class OrderSagaOrchestrator(
    OrderDbContext db,
    IPaymentClient paymentClient,
    IOptions<RabbitMqOptions> rabbitMqOptions,
    ILogger<OrderSagaOrchestrator> logger)
{
    private DateTime _lastTimestamp = DateTime.MinValue;

    private int MaxRetries => rabbitMqOptions.Value.MaxRetryCount;

    // ================================================================ StockReservationFailed

    public async Task HandleStockReservationFailedAsync(
        StockReservationFailed failed,
        ConsumedMessage message,
        string consumer,
        CancellationToken cancellationToken)
    {
        if (failed.OrderId == Guid.Empty)
        {
            throw new PoisonMessageException("StockReservationFailed has an empty orderId.");
        }

        if (await IsAlreadyProcessedAsync(message, failed.OrderId, cancellationToken))
        {
            return;
        }

        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == failed.OrderId, cancellationToken);

        if (order is null)
        {
            logger.LogWarning(
                "StockReservationFailed {MessageId} for unknown order {OrderId}: nothing to do (acked).",
                message.MessageId,
                failed.OrderId);
            await MarkProcessedAndSaveAsync(message, consumer, cancellationToken);
            return;
        }

        if (order.Status != OrderStatus.Pending)
        {
            logger.LogWarning(
                "Ignoring StockReservationFailed {MessageId}: order {OrderId} is already {Status}.",
                message.MessageId,
                order.Id,
                order.Status);
            await MarkProcessedAndSaveAsync(message, consumer, cancellationToken);
            return;
        }

        order.AddHistory(
            OrderStatus.Pending,
            $"Inventory could not reserve the stock: {failed.Reason} Nothing was reserved, so no compensation is needed.",
            NextTimestamp());

        CancelOrder(order, failed.Reason, message, consumer);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogWarning("Order {OrderId} CANCELLED (stock): {Reason}", order.Id, failed.Reason);
    }

    // ================================================================ StockReserved -> payment

    public async Task HandleStockReservedAsync(
        StockReserved reserved,
        ConsumedMessage message,
        string consumer,
        CancellationToken cancellationToken)
    {
        if (reserved.OrderId == Guid.Empty)
        {
            throw new PoisonMessageException("StockReserved has an empty orderId.");
        }

        if (await IsAlreadyProcessedAsync(message, reserved.OrderId, cancellationToken))
        {
            return;
        }

        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == reserved.OrderId, cancellationToken);

        if (order is null)
        {
            // Stock is held for an order this service does not know (e.g. the Order database was reset). Give it back.
            logger.LogWarning(
                "StockReserved {MessageId} for unknown order {OrderId}: sending ReleaseStock so the stock is not held forever.",
                message.MessageId,
                reserved.OrderId);

            var now = NextTimestamp();
            OutboxWriter.Add(
                db,
                new ReleaseStock(reserved.OrderId, "Order not found in the Order Service.")
                {
                    CorrelationId = message.CorrelationId,
                    OccurredAtUtc = now
                },
                EventRoutingKeys.ReleaseStock);
            MarkProcessed(message, consumer, now);
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        switch (order.Status)
        {
            case OrderStatus.Pending:
                {
                    var lines = string.Join(", ", reserved.Lines.Select(l => $"{l.Quantity} x {l.Sku}"));

                    order.TransitionTo(
                        OrderStatus.StockReserved,
                        $"Stock reserved by the Inventory Service: {lines}.",
                        NextTimestamp());
                    order.TransitionTo(
                        OrderStatus.PaymentProcessing,
                        $"Charging {order.TotalAmount:0.00} {order.Currency} to card ****{order.CardLast4} through the Payment Service.",
                        NextTimestamp());

                    // Persist the saga state BEFORE the HTTP call: a retry or restart resumes at the payment step.
                    await db.SaveChangesAsync(cancellationToken);

                    logger.LogInformation(
                        "Order {OrderId}: stock reserved ({Lines}). Saga step: charging {TotalAmount} {Currency}.",
                        order.Id,
                        lines,
                        order.TotalAmount,
                        order.Currency);
                    break;
                }

            case OrderStatus.StockReserved or OrderStatus.PaymentProcessing:
                logger.LogInformation(
                    "Order {OrderId} is {Status}: resuming the payment step (retry {RetryCount}).",
                    order.Id,
                    order.Status,
                    message.RetryCount);
                break;

            default:
                logger.LogWarning(
                    "Ignoring StockReserved {MessageId}: order {OrderId} is already {Status}.",
                    message.MessageId,
                    order.Id,
                    order.Status);
                await MarkProcessedAndSaveAsync(message, consumer, cancellationToken);
                return;
        }

        await RunPaymentStepAsync(order, message, consumer, cancellationToken);
    }

    private async Task RunPaymentStepAsync(Order order, ConsumedMessage message, string consumer, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(order.PaymentToken))
        {
            var missing = "No card reference is stored for this order, so it cannot be charged.";
            order.AddHistory(OrderStatus.PaymentProcessing, missing, NextTimestamp());
            CompensateAndCancel(order, missing, message, consumer);
            await db.SaveChangesAsync(cancellationToken);
            logger.LogWarning("Order {OrderId} CANCELLED: {Reason}", order.Id, missing);
            return;
        }

        var charge = await paymentClient.ChargeAsync(
            new PaymentChargeRequest(order.Id, order.TotalAmount, order.Currency, order.PaymentToken),
            cancellationToken);

        switch (charge.Status)
        {
            case PaymentCallStatus.Charged:
                {
                    var payment = charge.Payment!;
                    order.PaymentId = payment.Id;
                    order.PaymentToken = null;
                    order.AddHistory(
                        OrderStatus.PaymentProcessing,
                        $"Payment {payment.Id} charged: {payment.Amount:0.00} {payment.Currency} (card ****{payment.CardLast4}).",
                        NextTimestamp());

                    if (order.SimulateFailureAfterPayment)
                    {
                        await RefundAfterSimulatedFailureAsync(order, payment, message, consumer, cancellationToken);
                    }
                    else
                    {
                        ConfirmOrder(order, payment, message, consumer);
                    }

                    break;
                }

            case PaymentCallStatus.Refunded:
                {
                    // An earlier attempt already refunded (its final save was lost). Finish the compensation.
                    var payment = charge.Payment!;
                    order.PaymentId = payment.Id;
                    order.AddHistory(
                        OrderStatus.PaymentProcessing,
                        $"Payment {payment.Id} had already been refunded by an earlier attempt.",
                        NextTimestamp());
                    CompensateAndCancel(
                        order,
                        order.SimulateFailureAfterPayment
                            ? "Failure after payment (simulated demo). The payment was refunded and the stock released."
                            : $"Payment {payment.Id} was refunded.",
                        message,
                        consumer);
                    break;
                }

            case PaymentCallStatus.Declined:
                {
                    order.PaymentId = charge.Payment?.Id;
                    var declined = $"Payment declined: {charge.Error}";
                    order.AddHistory(OrderStatus.PaymentProcessing, declined, NextTimestamp());
                    CompensateAndCancel(order, declined, message, consumer);
                    break;
                }

            case PaymentCallStatus.Unavailable:
                {
                    if (message.RetryCount < MaxRetries)
                    {
                        // Nothing saved in this attempt: the consumer retries 1 s / 2 s / 4 s and resumes here.
                        throw new SagaTransientException(
                            $"Payment Service unavailable while charging order {order.Id}: {charge.Error}");
                    }

                    // Last attempt: do not leave the order stuck - compensate. The charge is idempotent by OrderId,
                    // so if it did go through, the Payment Service shows it under this OrderId (README known issue).
                    var unavailable = $"Payment Service unavailable after {MaxRetries + 1} attempts: {charge.Error}";
                    order.AddHistory(OrderStatus.PaymentProcessing, unavailable, NextTimestamp());
                    CompensateAndCancel(order, unavailable, message, consumer);
                    break;
                }

            default:
                {
                    // Rejected (400/409) or NotFound: permanent - retrying the same request cannot succeed.
                    var rejected = $"Payment rejected by the Payment Service: {charge.Error}";
                    order.AddHistory(OrderStatus.PaymentProcessing, rejected, NextTimestamp());
                    CompensateAndCancel(order, rejected, message, consumer);
                    break;
                }
        }

        await db.SaveChangesAsync(cancellationToken);

        if (order.Status == OrderStatus.Confirmed)
        {
            logger.LogInformation(
                "Order {OrderId} CONFIRMED: payment {PaymentId}, total {TotalAmount} {Currency}. OrderConfirmed saved to the outbox.",
                order.Id,
                order.PaymentId,
                order.TotalAmount,
                order.Currency);
        }
        else
        {
            logger.LogWarning(
                "Order {OrderId} CANCELLED after compensation: {Reason}",
                order.Id,
                order.FailureReason);
        }
    }

    // ================================================================ outcomes

    private void ConfirmOrder(Order order, PaymentDto payment, ConsumedMessage message, string consumer)
    {
        var now = NextTimestamp();
        order.FailureReason = null;
        order.TransitionTo(
            OrderStatus.Confirmed,
            $"Order confirmed. Payment {payment.Id} captured; the Inventory Service commits the reserved stock.",
            now);

        OutboxWriter.Add(
            db,
            new OrderConfirmed(order.Id, payment.Id, order.TotalAmount, order.CustomerEmail)
            {
                CorrelationId = order.CorrelationId,
                OccurredAtUtc = now
            },
            EventRoutingKeys.OrderConfirmed);

        MarkProcessed(message, consumer, now);
    }

    private async Task RefundAfterSimulatedFailureAsync(
        Order order,
        PaymentDto payment,
        ConsumedMessage message,
        string consumer,
        CancellationToken cancellationToken)
    {
        order.AddHistory(
            OrderStatus.PaymentProcessing,
            "Simulated failure after payment (demo switch). Compensation starts: refund the payment, then release the stock.",
            NextTimestamp());

        logger.LogWarning(
            "Order {OrderId}: simulated failure AFTER payment {PaymentId}. Compensating: refund, release stock, cancel.",
            order.Id,
            payment.Id);

        var refund = await paymentClient.RefundAsync(
            payment.Id,
            "Saga compensation: simulated failure after payment.",
            cancellationToken);

        if (refund.Status != PaymentCallStatus.Refunded)
        {
            // The customer was charged and not refunded yet: never cancel silently.
            // Retry; when retries are exhausted the message goes to the DLQ for manual replay.
            throw new SagaTransientException(
                $"Refund of payment {payment.Id} for order {order.Id} failed ({refund.Status}): {refund.Error}");
        }

        order.AddHistory(
            OrderStatus.PaymentProcessing,
            $"Compensation: payment {payment.Id} refunded ({payment.Amount:0.00} {payment.Currency}).",
            NextTimestamp());

        CompensateAndCancel(
            order,
            "Failure after payment (simulated demo). The payment was refunded and the stock released.",
            message,
            consumer);
    }

    /// <summary>Stock was reserved: send the ReleaseStock compensation command, then cancel.</summary>
    private void CompensateAndCancel(Order order, string reason, ConsumedMessage message, string consumer)
    {
        var releaseAt = NextTimestamp();

        OutboxWriter.Add(
            db,
            new ReleaseStock(order.Id, reason)
            {
                CorrelationId = order.CorrelationId,
                OccurredAtUtc = releaseAt
            },
            EventRoutingKeys.ReleaseStock);

        order.AddHistory(
            order.Status,
            "Compensation: ReleaseStock command sent to the Inventory Service (the reserved units become available again).",
            releaseAt);

        CancelOrder(order, reason, message, consumer);
    }

    private void CancelOrder(Order order, string reason, ConsumedMessage message, string consumer)
    {
        var now = NextTimestamp();

        order.PaymentToken = null;
        order.FailureReason = OrderTransitions.Truncate(reason);
        order.TransitionTo(OrderStatus.Cancelled, $"Order cancelled: {reason}", now);

        OutboxWriter.Add(
            db,
            new OrderCancelled(order.Id, order.FailureReason, order.CustomerEmail)
            {
                CorrelationId = order.CorrelationId,
                OccurredAtUtc = now
            },
            EventRoutingKeys.OrderCancelled);

        MarkProcessed(message, consumer, now);
    }

    // ================================================================ idempotency helpers

    private async Task<bool> IsAlreadyProcessedAsync(ConsumedMessage message, Guid orderId, CancellationToken cancellationToken)
    {
        var processed = await db.ProcessedMessages
            .AsNoTracking()
            .AnyAsync(m => m.MessageId == message.MessageId, cancellationToken);

        if (processed)
        {
            logger.LogInformation(
                "Duplicate {MessageType} {MessageId} for order {OrderId}: already processed, nothing changed.",
                message.MessageType,
                message.MessageId,
                orderId);
        }

        return processed;
    }

    private void MarkProcessed(ConsumedMessage message, string consumer, DateTime nowUtc)
    {
        db.ProcessedMessages.Add(new ProcessedMessage
        {
            MessageId = message.MessageId,
            Consumer = consumer,
            ProcessedAtUtc = nowUtc
        });
    }

    private async Task MarkProcessedAndSaveAsync(ConsumedMessage message, string consumer, CancellationToken cancellationToken)
    {
        MarkProcessed(message, consumer, NextTimestamp());
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Strictly increasing UTC timestamps, so timeline entries and outbox events keep their order.</summary>
    private DateTime NextTimestamp()
    {
        var now = DateTime.UtcNow;
        if (now <= _lastTimestamp)
        {
            now = _lastTimestamp.AddTicks(1);
        }

        _lastTimestamp = now;
        return now;
    }
}