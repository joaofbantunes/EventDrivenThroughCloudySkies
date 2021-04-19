using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace BurgerJoint.Events.AzureEventHubs
{
    public class EventHubsOrderEventConsumer : IOrderEventConsumer
    {
        private readonly ILogger<EventHubsOrderEventConsumer> _logger;

        private static readonly JsonSerializerSettings Settings = new()
        {
            TypeNameHandling = TypeNameHandling.All
        };

        private readonly EventProcessorClient _client;

        public EventHubsOrderEventConsumer(
            EventHubsOrderEventConsumerSettings settings,
            ILogger<EventHubsOrderEventConsumer> logger)
        {
            _logger = logger;
          
            var checkpointStore = new BlobContainerClient(
                settings.CheckpointStoreConnectionString,
                settings.CheckpointStoreName);

            _client = new EventProcessorClient(checkpointStore, settings.ConsumerGroup, settings.ConnectionString);
        }

        public async Task Subscribe(Func<OrderEventBase, Task> callback, CancellationToken ct)
        {
            _client.ProcessEventAsync += eventArgs =>
            {
                var @event = JsonConvert.DeserializeObject<OrderEventBase>(
                    Encoding.UTF8.GetString(eventArgs.Data.EventBody),
                    Settings);

                return callback(@event);
            };

            _client.ProcessErrorAsync += errorArgs =>
            {
                _logger.LogError(errorArgs.Exception, "An error occurred processing event, we should handle it!");
                return Task.CompletedTask;
            };

            await _client.StartProcessingAsync(ct);

            await AwaitCancellationAsync(ct);

            await _client.StopProcessingAsync(ct);
        }

        private static Task AwaitCancellationAsync(CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<bool>();
            ct.Register(s => ((TaskCompletionSource<bool>) s).SetResult(true), tcs);
            return tcs.Task;
        }
    }
}