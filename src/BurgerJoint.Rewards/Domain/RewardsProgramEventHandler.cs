using System.Threading.Tasks;
using BurgerJoint.Events;
using BurgerJoint.Rewards.Data;
using BurgerJoint.Rewards.Features.Dashboard;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BurgerJoint.Rewards.Domain
{
    // it should go without saying this is very very far from how something like this should be implemented 🙂

    public class RewardsProgramEventHandler : IOrderEventHandler
    {
        private readonly RewardsDbContext _db;
        private readonly IHubContext<DashboardHub, IDashboardHub> _hub;

        public RewardsProgramEventHandler(RewardsDbContext db, IHubContext<DashboardHub, IDashboardHub> hub)
            => (_db, _hub) = (db, hub);

        public Task HandleAsync(OrderEventBase @event)
            => @event switch
            {
                OrderDelivered delivered => HandleOrderDeliveredAsync(delivered),
                _ => Task.CompletedTask // this particular service only cares about successfully completed orders
            };

        private async Task HandleOrderDeliveredAsync(OrderDelivered delivered)
        {
            _db.CustomerPurchases.Add(new CustomerPurchase(delivered.CustomerNumber, delivered.OccurredAt));
            await _db.SaveChangesAsync();

            var purchaseCount = await _db
                .CustomerPurchases
                .CountAsync(p => p.CustomerNumber == delivered.CustomerNumber);

            if (purchaseCount % 3 == 0)
            {
                await _hub.Clients.All.ReceiveEvent(
                    new EventInfo(
                        delivered.OccurredAt,
                        $"Customer has bought {purchaseCount} burgers. Maybe it's time for a reward!"));
            }
        }
    }
}