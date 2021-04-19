using Microsoft.Extensions.DependencyInjection;

namespace BurgerJoint.Events.Kafka
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddKafkaEventPublisher(this IServiceCollection services)
            => services.AddSingleton<IOrderEventPublisher, KafkaOrderEventPublisher>();

        public static IServiceCollection AddKafkaEventConsumer(
            this IServiceCollection services,
            KafkaOrderEventConsumerSettings settings)
            => services
                .AddSingleton(settings)
                .AddSingleton<IOrderEventConsumer, KafkaOrderEventConsumer>();
    }
}