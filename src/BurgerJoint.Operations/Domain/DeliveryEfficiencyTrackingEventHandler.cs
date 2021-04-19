using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using BurgerJoint.Events;
using BurgerJoint.Operations.Features.Dashboard;
using Microsoft.AspNetCore.SignalR;

namespace BurgerJoint.Operations.Domain
{
    // it should go without saying this is very very far from how something like this should be implemented 🙂

    public class DeliveryEfficiencyTrackingEventHandler : IOrderEventHandler
    {
        private readonly IHubContext<DashboardHub, IDashboardHub> _hub;
        private readonly ConcurrentDictionary<Guid, DateTime> _orderCreatedMap;

        public DeliveryEfficiencyTrackingEventHandler(
            IHubContext<DashboardHub, IDashboardHub> hub)
        {
            _hub = hub;
            _orderCreatedMap = new ConcurrentDictionary<Guid, DateTime>();
        }

        public Task HandleAsync(OrderEventBase @event)
            => @event switch
            {
                OrderCreated created => HandleOrderCreatedAsync(created),
                OrderDelivered delivered => HandleOrderDeliveredAsync(delivered),
                OrderCancelled cancelled => HandleOrderCancelledAsync(cancelled),
                _ => Task.CompletedTask // new events can be ignored, if we're interested we should handle them
            };

        private Task HandleOrderCreatedAsync(OrderCreated created)
        {
            // if the event already exists, means it was a retry, we can safely ignore
            _orderCreatedMap.TryAdd(created.OrderId, created.OccurredAt);
            return Task.CompletedTask;
        }

        private async Task HandleOrderDeliveredAsync(OrderDelivered delivered)
        {
            if (_orderCreatedMap.TryRemove(delivered.OrderId, out var orderCreatedAt))
            {
                var timeToDeliver = delivered.OccurredAt.Subtract(orderCreatedAt);

                if (timeToDeliver > TimeSpan.FromSeconds(30))
                {
                    await _hub.Clients.All.ReceiveEvent(
                        new EventInfo(
                            delivered.OccurredAt,
                            $"Order {delivered.OrderId} took {timeToDeliver} to complete. We need to hire more people!"));
                }
            }
        }

        private Task HandleOrderCancelledAsync(OrderCancelled cancelled)
        {
            _orderCreatedMap.TryRemove(cancelled.OrderId, out _);
            return Task.CompletedTask;
        }
    }
}