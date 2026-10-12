using ECommerceSystem.InventoryService.Data;
using ECommerceSystem.InventoryService.Data.Entities;
using ECommerceSystem.Shared.Events;
using ECommerceSystem.Shared.Messaging;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ECommerceSystem.InventoryService.Services;

/// <summary>
/// Saga participant logic of the Inventory Service. Every handler:
///  - is idempotent: the incoming MessageId is inserted into ProcessedMessages in the SAME SaveChanges
///    (one local transaction) as the stock change; an already-processed MessageId changes nothing,
///  - protects stock with StockItems.RowVersion (concurrent updates are retried with fresh data),
///  - treats invalid data as a poison message (dead-lettered, never retried).
/// </summary>
public sealed class InventoryReservationService(
    InventoryDbContext db,
    IMessagePublisher publisher,
    ILogger<InventoryReservationService> logger)
{
    private const int MaxConcurrencyAttempts = 3;
    private const string ResultMessagePurpose = "stock-reservation-result";

    // ------------------------------------------------------------------ OrderPlaced -> reserve all or none

    public async Task HandleOrderPlacedAsync(OrderPlaced orderPlaced, ConsumedMessage message, string consumer, CancellationToken cancellationToken)
    {
        ValidateOrderPlaced(orderPlaced);

        if (await IsAlreadyProcessedAsync(message.MessageId, cancellationToken))
        {
            logger.LogInformation(
                "Duplicate OrderPlaced {MessageId} for order {OrderId}: already processed, stock NOT changed again. Re-publishing the recorded outcome.",
                message.MessageId,
                orderPlaced.OrderId);

            await PublishOutcomeFromStateAsync(orderPlaced, message, cancellationToken);
            return;
        }

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                var outcome = await ReserveAllOrNoneAsync(orderPlaced, message, consumer, cancellationToken);
                PublishOutcome(orderPlaced.OrderId, outcome, message);
                return;
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxConcurrencyAttempts)
            {
                db.ChangeTracker.Clear();
                logger.LogWarning(
                    "Concurrent stock change detected while reserving order {OrderId} (attempt {Attempt} of {MaxAttempts}). Retrying with fresh data.",
                    orderPlaced.OrderId,
                    attempt,
                    MaxConcurrencyAttempts);
            }
            catch (DbUpdateException ex) when (IsDuplicateKey(ex))
            {
                db.ChangeTracker.Clear();
                logger.LogInformation(
                    "OrderPlaced {MessageId} for order {OrderId} was already applied by a concurrent delivery. Re-publishing the recorded outcome.",
                    message.MessageId,
                    orderPlaced.OrderId);

                await PublishOutcomeFromStateAsync(orderPlaced, message, cancellationToken);
                return;
            }
        }
    }

    private async Task<ReservationOutcome> ReserveAllOrNoneAsync(
        OrderPlaced orderPlaced,
        ConsumedMessage message,
        string consumer,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        // Business-key idempotency: this order already holds reservations (e.g. delivered under another MessageId).
        var existing = await db.StockReservations
            .Where(r => r.OrderId == orderPlaced.OrderId)
            .ToListAsync(cancellationToken);

        if (existing.Count > 0)
        {
            RecordProcessed(message.MessageId, consumer, now);
            await db.SaveChangesAsync(cancellationToken);

            return ReservationOutcome.Reserved(existing
                .Select(r => new OrderLine(r.ProductId, SkuFor(orderPlaced, r.ProductId), r.Quantity))
                .ToList());
        }

        // Merge duplicate lines of the same product.
        var requested = orderPlaced.Lines
            .GroupBy(l => l.ProductId)
            .Select(g => new OrderLine(g.Key, g.First().Sku, g.Sum(l => l.Quantity)))
            .ToList();

        var productIds = requested.Select(l => l.ProductId).ToList();

        var stockItems = await db.StockItems
            .Where(s => productIds.Contains(s.ProductId))
            .ToDictionaryAsync(s => s.ProductId, cancellationToken);

        var shortages = new List<StockShortage>();
        foreach (var line in requested)
        {
            if (!stockItems.TryGetValue(line.ProductId, out var item))
            {
                shortages.Add(new StockShortage(line.ProductId, line.Sku, line.Quantity, 0));
            }
            else if (item.QuantityAvailable < line.Quantity)
            {
                shortages.Add(new StockShortage(line.ProductId, item.Sku, line.Quantity, item.QuantityAvailable));
            }
        }

        if (shortages.Count > 0)
        {
            // NONE reserved. Still record the message as processed (same transaction) so a redelivery is a no-op.
            RecordProcessed(message.MessageId, consumer, now);
            await db.SaveChangesAsync(cancellationToken);

            var reason = "Insufficient stock: " + string.Join("; ", shortages.Select(s =>
                $"{(string.IsNullOrEmpty(s.Sku) ? "product " + s.ProductId : s.Sku)} requested {s.Requested}, available {s.Available}"));

            return ReservationOutcome.Failed(reason, shortages);
        }

        // ALL reserved in this single SaveChanges (one local transaction).
        foreach (var line in requested)
        {
            var item = stockItems[line.ProductId];
            item.QuantityReserved += line.Quantity;
            item.UpdatedAtUtc = now;

            db.StockReservations.Add(new StockReservation
            {
                OrderId = orderPlaced.OrderId,
                ProductId = line.ProductId,
                Quantity = line.Quantity,
                Status = ReservationStatus.Reserved,
                CreatedAtUtc = now
            });
        }

        RecordProcessed(message.MessageId, consumer, now);
        await db.SaveChangesAsync(cancellationToken);

        return ReservationOutcome.Reserved(requested
            .Select(l => new OrderLine(l.ProductId, stockItems[l.ProductId].Sku, l.Quantity))
            .ToList());
    }

    // ------------------------------------------------------------------ OrderConfirmed -> commit

    public Task HandleOrderConfirmedAsync(OrderConfirmed orderConfirmed, ConsumedMessage message, string consumer, CancellationToken cancellationToken)
    {
        if (orderConfirmed.OrderId == Guid.Empty)
        {
            throw new PoisonMessageException("OrderConfirmed has an empty orderId.");
        }

        return TransitionIdempotentlyAsync(orderConfirmed.OrderId, message, consumer, ReservationStatus.Committed, removeFromOnHand: true, cancellationToken);
    }

    // ------------------------------------------------------------------ ReleaseStock (compensation) -> release

    public Task HandleReleaseStockAsync(ReleaseStock releaseStock, ConsumedMessage message, string consumer, CancellationToken cancellationToken)
    {
        if (releaseStock.OrderId == Guid.Empty)
        {
            throw new PoisonMessageException("ReleaseStock has an empty orderId.");
        }

        logger.LogInformation("Compensation requested for order {OrderId}: {Reason}", releaseStock.OrderId, releaseStock.Reason);

        return TransitionIdempotentlyAsync(releaseStock.OrderId, message, consumer, ReservationStatus.Released, removeFromOnHand: false, cancellationToken);
    }

    private async Task TransitionIdempotentlyAsync(
        Guid orderId,
        ConsumedMessage message,
        string consumer,
        ReservationStatus newStatus,
        bool removeFromOnHand,
        CancellationToken cancellationToken)
    {
        if (await IsAlreadyProcessedAsync(message.MessageId, cancellationToken))
        {
            logger.LogInformation(
                "Duplicate {MessageType} {MessageId} for order {OrderId}: already processed, nothing changed.",
                message.MessageType,
                message.MessageId,
                orderId);
            return;
        }

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                var count = await TransitionReservationsAsync(orderId, message, consumer, newStatus, removeFromOnHand, cancellationToken);

                if (count == 0)
                {
                    logger.LogInformation(
                        "Order {OrderId} has no Reserved stock - nothing to change ({NewStatus} is idempotent).",
                        orderId,
                        newStatus);
                }
                else
                {
                    logger.LogInformation(
                        "{NewStatus} {Count} reservation(s) for order {OrderId}.",
                        newStatus,
                        count,
                        orderId);
                }

                return;
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxConcurrencyAttempts)
            {
                db.ChangeTracker.Clear();
                logger.LogWarning(
                    "Concurrent stock change while setting order {OrderId} to {NewStatus} (attempt {Attempt} of {MaxAttempts}). Retrying.",
                    orderId,
                    newStatus,
                    attempt,
                    MaxConcurrencyAttempts);
            }
            catch (DbUpdateException ex) when (IsDuplicateKey(ex))
            {
                db.ChangeTracker.Clear();
                logger.LogInformation("{MessageId} was already applied by a concurrent delivery.", message.MessageId);
                return;
            }
        }
    }

    private async Task<int> TransitionReservationsAsync(
        Guid orderId,
        ConsumedMessage message,
        string consumer,
        ReservationStatus newStatus,
        bool removeFromOnHand,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var reservations = await db.StockReservations
            .Where(r => r.OrderId == orderId && r.Status == ReservationStatus.Reserved)
            .ToListAsync(cancellationToken);

        var productIds = reservations.Select(r => r.ProductId).Distinct().ToList();

        var stockItems = await db.StockItems
            .Where(s => productIds.Contains(s.ProductId))
            .ToDictionaryAsync(s => s.ProductId, cancellationToken);

        foreach (var reservation in reservations)
        {
            if (stockItems.TryGetValue(reservation.ProductId, out var item))
            {
                item.QuantityReserved -= reservation.Quantity;

                if (removeFromOnHand)
                {
                    item.QuantityOnHand -= reservation.Quantity;
                }

                item.UpdatedAtUtc = now;
            }
            else
            {
                logger.LogWarning(
                    "Stock item for product {ProductId} no longer exists; only the reservation of order {OrderId} is updated.",
                    reservation.ProductId,
                    orderId);
            }

            reservation.Status = newStatus;
            reservation.UpdatedAtUtc = now;
        }

        RecordProcessed(message.MessageId, consumer, now);
        await db.SaveChangesAsync(cancellationToken);

        return reservations.Count;
    }

    // ------------------------------------------------------------------ helpers

    private Task<bool> IsAlreadyProcessedAsync(Guid messageId, CancellationToken cancellationToken) =>
        db.ProcessedMessages.AsNoTracking().AnyAsync(m => m.MessageId == messageId, cancellationToken);

    private void RecordProcessed(Guid messageId, string consumer, DateTime now) =>
        db.ProcessedMessages.Add(new ProcessedMessage
        {
            MessageId = messageId,
            Consumer = consumer,
            ProcessedAtUtc = now
        });

    private async Task PublishOutcomeFromStateAsync(OrderPlaced orderPlaced, ConsumedMessage message, CancellationToken cancellationToken)
    {
        var reservations = await db.StockReservations
            .AsNoTracking()
            .Where(r => r.OrderId == orderPlaced.OrderId)
            .ToListAsync(cancellationToken);

        var outcome = reservations.Count > 0
            ? ReservationOutcome.Reserved(reservations
                .Select(r => new OrderLine(r.ProductId, SkuFor(orderPlaced, r.ProductId), r.Quantity))
                .ToList())
            : ReservationOutcome.Failed("Stock reservation was already rejected for this order.", []);

        PublishOutcome(orderPlaced.OrderId, outcome, message);
    }

    private void PublishOutcome(Guid orderId, ReservationOutcome outcome, ConsumedMessage source)
    {
        // Same input message -> same result MessageId, so a re-publish is deduplicated by the Order Service.
        var resultMessageId = DeterministicGuid.Create(source.MessageId, ResultMessagePurpose);

        if (outcome.IsReserved)
        {
            publisher.PublishEvent(
                new StockReserved(orderId, outcome.Lines)
                {
                    MessageId = resultMessageId,
                    CorrelationId = source.CorrelationId
                },
                EventRoutingKeys.StockReserved);

            logger.LogInformation(
                "Stock RESERVED for order {OrderId}: {Lines}. StockReserved {ResultMessageId} published.",
                orderId,
                string.Join(", ", outcome.Lines.Select(l => $"{l.Quantity} x {l.Sku}")),
                resultMessageId);
        }
        else
        {
            publisher.PublishEvent(
                new StockReservationFailed(orderId, outcome.Reason, outcome.Shortages)
                {
                    MessageId = resultMessageId,
                    CorrelationId = source.CorrelationId
                },
                EventRoutingKeys.StockReservationFailed);

            logger.LogWarning(
                "Stock reservation FAILED for order {OrderId}: {Reason}. Nothing was reserved. StockReservationFailed {ResultMessageId} published.",
                orderId,
                outcome.Reason,
                resultMessageId);
        }
    }

    private static void ValidateOrderPlaced(OrderPlaced orderPlaced)
    {
        if (orderPlaced.OrderId == Guid.Empty)
        {
            throw new PoisonMessageException("OrderPlaced has an empty orderId.");
        }

        if (orderPlaced.Lines is null || orderPlaced.Lines.Count == 0)
        {
            throw new PoisonMessageException($"OrderPlaced for order {orderPlaced.OrderId} has no lines.");
        }

        if (orderPlaced.Lines.Any(l => l.ProductId <= 0 || l.Quantity <= 0))
        {
            throw new PoisonMessageException($"OrderPlaced for order {orderPlaced.OrderId} has a line with an invalid productId or quantity.");
        }
    }

    private static string SkuFor(OrderPlaced orderPlaced, int productId) =>
        orderPlaced.Lines.FirstOrDefault(l => l.ProductId == productId)?.Sku ?? string.Empty;

    private static bool IsDuplicateKey(DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: 2601 or 2627 };
}