using Microsoft.Extensions.DependencyInjection;

namespace BurgerJoint.Events.AzureEventHubs
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAzureEventHubsPublisher(
            this IServiceCollection services,
            EventHubsOrderEventPublisherSettings settings)
            => services
                .AddSingleton(settings)
                .AddSingleton<IOrderEventPublisher, EventHubsOrderEventPublisher>();

        public static IServiceCollection AddAzureEventHubsConsumer(
            this IServiceCollection services,
            EventHubsOrderEventConsumerSettings settings)
            => services
                .AddSingleton(settings)
                .AddSingleton<IOrderEventConsumer, EventHubsOrderEventConsumer>();
    }
}