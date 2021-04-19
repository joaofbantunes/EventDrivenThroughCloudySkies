using System;
using System.Threading.Tasks;
using BurgerJoint.Events;
using BurgerJoint.Rewards.Data;
using BurgerJoint.Rewards.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BurgerJoint.Rewards.Infrastructure
{
    public class OrderEventHandlerIdempotenceDecorator : IOrderEventHandler
    {
        private readonly IOrderEventHandler _next;
        private readonly RewardsDbContext _db;
        private readonly ILogger<OrderEventHandlerIdempotenceDecorator> _logger;

        public OrderEventHandlerIdempotenceDecorator(
            IOrderEventHandler next, 
            RewardsDbContext db,
            ILogger<OrderEventHandlerIdempotenceDecorator> logger)
        {
            _next = next;
            _db = db;
            _logger = logger;
        }

        public async Task HandleAsync(OrderEventBase @event)
        {
            try
            {           
                await _db.Database.BeginTransactionAsync();

                if (!await TryAddHandledEvent(@event.Id))
                {
                    // if event already handled, just rollback and return
                    _logger.LogInformation("Event {eventId} already handled, ignoring.", @event.Id);
                    await _db.Database.RollbackTransactionAsync();
                    return;
                }
                
                // note that if the actual HandleAsync is slow, this approach is not the best idea,
                // as the transaction will be open for too long
                await _next.HandleAsync(@event);

                await _db.Database.CommitTransactionAsync();
            }
            catch (Exception)
            {
                await _db.Database.RollbackTransactionAsync();
                throw;
            }
        }

        private async Task<bool> TryAddHandledEvent(Guid eventId)
        {
            const int PrimaryKeyViolationErrorNumber = 2627;
            try
            {
                _db.HandledEvents.Add(HandledEvent.Create(eventId));
                await _db.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException {Number: PrimaryKeyViolationErrorNumber})
            {
                return false;
            }
        }
    }
}
