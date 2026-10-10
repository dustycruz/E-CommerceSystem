using ECommerceSystem.Shared.Correlation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ECommerceSystem.Shared.Messaging;

public static class MessagingExtensions
{
    /// <summary>
    /// Registers RabbitMQ options (section "RabbitMq"), the shared connection and the confirming publisher.
    /// Consumers are added by each service with AddHostedService&lt;TConsumer&gt;().
    /// </summary>
    public static IServiceCollection AddRabbitMqMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddECommerceCorrelation();
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.TryAddSingleton<IRabbitMqConnectionProvider, RabbitMqConnectionProvider>();
        services.TryAddSingleton<IMessagePublisher, RabbitMqMessagePublisher>();
        return services;
    }
}