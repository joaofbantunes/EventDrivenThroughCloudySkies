using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Newtonsoft.Json;

namespace BurgerJoint.Events.AzureEventHubs
{
    public class EventHubsOrderEventPublisher : IOrderEventPublisher, IAsyncDisposable
    {
        private static readonly JsonSerializerSettings Settings = new()
        {
            TypeNameHandling = TypeNameHandling.All
        };
        
        private readonly EventHubProducerClient _client;

        public EventHubsOrderEventPublisher(EventHubsOrderEventPublisherSettings settings)
            => _client = new EventHubProducerClient(settings.ConnectionString);

        // supporting batch publishing would benefit performance
        public async Task PublishAsync(OrderEventBase orderEventBase, CancellationToken ct)
        {
            var eventData =
                new EventData(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(orderEventBase, Settings)));

            await _client.SendAsync(
                new[] {eventData},
                new SendEventOptions
                {
                    // use order id as key so EventHub maintains order between events for the same entity
                    PartitionKey = orderEventBase.OrderId.ToString()
                },
                ct);
        }
        
        public ValueTask DisposeAsync()
            => _client.DisposeAsync();
    }
}