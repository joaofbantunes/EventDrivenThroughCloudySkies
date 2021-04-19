namespace BurgerJoint.Events.Kafka
{
    public record KafkaOrderEventConsumerSettings
    {
        public string ConsumerGroup { get; init; }
    }
}