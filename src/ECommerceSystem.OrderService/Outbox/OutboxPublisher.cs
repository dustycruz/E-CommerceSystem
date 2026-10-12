using ECommerceSystem.OrderService.Data;
using ECommerceSystem.Shared.Correlation;
using ECommerceSystem.Shared.Messaging;
using Microsoft.EntityFrameworkCore;

namespace ECommerceSystem.OrderService.Outbox;

/// <summary>
/// Transactional outbox, publish side (hosted background service):
/// every second it reads pending rows (PublishedAtUtc IS NULL) in event order, publishes each one through
/// the confirming publisher (returns only after RabbitMQ confirmed it), then marks it published.
/// If RabbitMQ is down the row simply stays pending and is retried on the next tick - the order itself was
/// already accepted. At-least-once: a crash between confirm and SaveChanges re-publishes the same MessageId,
/// which consumers deduplicate (ProcessedMessages).
/// </summary>
public sealed class OutboxPublisher(
    IServiceScopeFactory scopeFactory,
    IMessagePublisher publisher,
    ILogger<OutboxPublisher> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);
    private const int BatchSize = 20;
    private const int MaxErrorLength = 2000;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Outbox publisher started (poll every {PollSeconds} s, batch size {BatchSize}).",
            PollInterval.TotalSeconds,
            BatchSize);

        using var timer = new PeriodicTimer(PollInterval);

        try
        {
            do
            {
                try
                {
                    await PublishPendingAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Outbox publisher cycle failed; it will run again in {PollSeconds} s.", PollInterval.TotalSeconds);
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
    }

    private async Task PublishPendingAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

        var pending = await db.OutboxMessages
            .Where(m => m.PublishedAtUtc == null)
            .OrderBy(m => m.OccurredAtUtc)
            .ThenBy(m => m.Id)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            return;
        }

        foreach (var message in pending)
        {
            message.Attempts++;

            try
            {
                publisher.Publish(new OutgoingMessage(
                    message.Id,
                    message.Type,
                    message.RoutingKey,
                    message.Payload,
                    message.CorrelationId)
                {
                    OccurredAtUtc = message.OccurredAtUtc
                });

                message.PublishedAtUtc = DateTime.UtcNow;
                message.LastError = null;
            }
            catch (Exception ex)
            {
                message.LastError = ex.Message.Length <= MaxErrorLength ? ex.Message : ex.Message[..MaxErrorLength];

                using (logger.BeginScope(CorrelationConstants.LogScopeFormat, message.CorrelationId))
                {
                    logger.LogWarning(
                        "Outbox message {MessageId} ({MessageType}) NOT published yet (attempt {Attempt}): {Error}. It stays in the outbox and is retried automatically.",
                        message.Id,
                        message.Type,
                        message.Attempts,
                        message.LastError);
                }

                // Stop this batch to keep the event order; the broker is probably unavailable.
                break;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}