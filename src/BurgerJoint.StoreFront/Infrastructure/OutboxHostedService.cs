using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BurgerJoint.StoreFront.Infrastructure
{
    public class OutboxHostedService : BackgroundService
    {
        private readonly OutboxMessagePublisher _publisher;
        private readonly OutboxMessageListener _listener;
        private readonly ILogger<OutboxHostedService> _logger;

        public OutboxHostedService(
            OutboxMessagePublisher publisher,
            OutboxMessageListener listener,
            ILogger<OutboxHostedService> logger)
        {
            _publisher = publisher;
            _listener = listener;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _publisher.PublishPendingAsync(stoppingToken);
                    await IdleAsync(stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // no need to log a TaskCanceledException, as it's normal behavior,
                    // the loop will be interrupted when checking for IsCancellationRequested
                }
                catch (Exception ex)
                {
                    // We don't want the background service to stop while the application continues,
                    // so catching and logging.
                    // Should certainly have some extra checks for the reasons, to act on it. 
                    _logger.LogWarning(ex, "Unexpected error while publishing pending outbox messages.");
                }
            }
        }
        
        private async Task IdleAsync(CancellationToken stoppingToken)
        {
            /*
              wait for whatever occurs first:
                - being notified of new messages added to the outbox
                - poll the outbox every N seconds, for example, in cases where another instance of the service persisted
                  something but didn't publish, or some error occurred when publishing and there are pending messages
            */

            using var idlingCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

            await Task.WhenAny(
                _listener.WaitForMessagesAsync(idlingCancellationTokenSource.Token),
                Task.Delay(TimeSpan.FromSeconds(30), idlingCancellationTokenSource.Token));

            // regardless of what caused the idling to be interrupted, cancel the other
            idlingCancellationTokenSource.Cancel();
        }
    }
}