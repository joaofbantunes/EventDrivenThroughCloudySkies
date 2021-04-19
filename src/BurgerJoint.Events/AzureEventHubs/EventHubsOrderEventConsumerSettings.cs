namespace BurgerJoint.Events.AzureEventHubs
{
    public record EventHubsOrderEventConsumerSettings
    {
        public string ConnectionString { get; init; }
        
        public string ConsumerGroup { get; init; }
        
        public string CheckpointStoreConnectionString { get; init; }
        
        public string CheckpointStoreName { get; init; }
    }
}