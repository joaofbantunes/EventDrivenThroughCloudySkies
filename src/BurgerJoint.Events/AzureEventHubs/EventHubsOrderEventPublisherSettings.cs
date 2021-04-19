namespace BurgerJoint.Events.AzureEventHubs
{
    public record EventHubsOrderEventPublisherSettings
    {
        public string ConnectionString { get; init; }
    }
}