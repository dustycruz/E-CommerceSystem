using System.Text.Json;
using ECommerceSystem.OrderService.Data;
using ECommerceSystem.OrderService.Data.Entities;
using ECommerceSystem.Shared.Events;

namespace ECommerceSystem.OrderService.Outbox;

/// <summary>
/// Transactional outbox, write side: the event is ADDED to the same DbContext as the order change, so the
/// caller's single SaveChanges commits both or neither. Nothing is sent to RabbitMQ here.
/// OutboxMessage.Id = event MessageId, so a re-published message is deduplicated by consumers.
/// </summary>
public static class OutboxWriter
{
    public static OutboxMessage Add<TEvent>(OrderDbContext db, TEvent integrationEvent, string routingKey)
        where TEvent : IntegrationEvent
    {
        var message = new OutboxMessage
        {
            Id = integrationEvent.MessageId,
            Type = typeof(TEvent).Name,
            RoutingKey = routingKey,
            Payload = JsonSerializer.Serialize(integrationEvent, EventJson.Options),
            CorrelationId = integrationEvent.CorrelationId,
            OccurredAtUtc = integrationEvent.OccurredAtUtc,
            PublishedAtUtc = null,
            Attempts = 0
        };

        db.OutboxMessages.Add(message);
        return message;
    }
}