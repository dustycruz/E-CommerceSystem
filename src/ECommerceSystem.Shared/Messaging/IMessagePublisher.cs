using ECommerceSystem.Shared.Events;

namespace ECommerceSystem.Shared.Messaging;

/// <summary>
/// Publishes messages with publisher confirms: the call returns only after RabbitMQ confirmed the message,
/// and throws if the broker nacked it or did not answer in time (the caller then retries later).
/// </summary>
public interface IMessagePublisher
{
    void Publish(OutgoingMessage message);

    void PublishEvent<TEvent>(TEvent integrationEvent, string routingKey) where TEvent : IntegrationEvent;
}