using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BurgerJoint.Events;
using BurgerJoint.StoreFront.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrderCancelled = BurgerJoint.StoreFront.Data.Events.OrderCancelled;
using OrderCreated = BurgerJoint.StoreFront.Data.Events.OrderCreated;
using OrderDelivered = BurgerJoint.StoreFront.Data.Events.OrderDelivered;

namespace BurgerJoint.StoreFront.Infrastructure
{
    public class OutboxMessagePublisher
    {
        private const int MaxBatchSize = 100;

        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<OutboxMessagePublisher> _logger;
        private readonly IOrderEventPublisher _eventPublisher;

        public OutboxMessagePublisher(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<OutboxMessagePublisher> logger,
            IOrderEventPublisher eventPublisher)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _eventPublisher = eventPublisher;
        }

        public async Task PublishPendingAsync(CancellationToken ct)
        {
            // Invokes PublishBatchAsync while batches are being published, to exhaust all pending messages.

            // ReSharper disable once EmptyEmbeddedStatement - the logic is part of the method invoked in the condition 
            while (!ct.IsCancellationRequested && await PublishBatchAsync(ct)) ;
        }

        // returns true if there is a new batch to publish, false otherwise
        private async Task<bool> PublishBatchAsync(CancellationToken ct)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BurgerDbContext>();

            await using var transaction = await db.Database.BeginTransactionAsync(ct);

            try
            {
                var messages = await GetMessageBatchAsync(db, ct);

                if (messages.Count > 0 && await TryDeleteMessagesAsync(db, messages, ct))
                {
                    foreach (var @event in messages.Select(MapEvent))
                    {
                        await _eventPublisher.PublishAsync(@event, ct);
                    }

                    // ReSharper disable once MethodSupportsCancellation - messages already published, try to delete them locally
                    await transaction.CommitAsync();

                    return await IsNewBatchAvailableAsync(db, ct);
                }

                await transaction.RollbackAsync(ct);

                // if we got here, there either aren't messages available or are being published concurrently
                // in either case, we can break the loop
                return false;
            }
            catch (Exception)
            {
                // ReSharper disable once MethodSupportsCancellation - try to clean up things before letting go
                await transaction.RollbackAsync();
                throw;
            }
        }

        private static Task<List<OutboxMessage>> GetMessageBatchAsync(BurgerDbContext db, CancellationToken ct)
            => db.Set<OutboxMessage>()
                .OrderBy(m => m.OccurredAt)
                .Take(MaxBatchSize)
                .ToListAsync(ct);

        private static Task<bool> IsNewBatchAvailableAsync(BurgerDbContext db, CancellationToken ct)
            => db.Set<OutboxMessage>().AnyAsync(ct);

        private async Task<bool> TryDeleteMessagesAsync(
            BurgerDbContext db,
            IReadOnlyCollection<OutboxMessage> messages,
            CancellationToken ct)
        {
            try
            {
                db.Set<OutboxMessage>().RemoveRange(messages);
                await db.SaveChangesAsync(ct);
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                _logger.LogDebug(
                    $"Delete messages [{string.Join(", ", messages.Select(m => m.Id))}] failed, as it was done concurrently.");
                return false;
            }
        }

        private static OrderEventBase MapEvent(OutboxMessage message)
            => message.OrderEventBase switch
            {
                OrderCreated created => new Events.OrderCreated
                {
                    Id = message.Id,
                    DishId = created.DishId,
                    OrderId = created.OrderId,
                    CustomerNumber = created.CustomerNumber,
                    OccurredAt = created.OccurredAt
                },
                OrderDelivered delivered => new Events.OrderDelivered
                {
                    Id = message.Id,
                    DishId = delivered.DishId,
                    OrderId = delivered.OrderId,
                    CustomerNumber = delivered.CustomerNumber,
                    OccurredAt = delivered.OccurredAt
                },
                OrderCancelled cancelled => new Events.OrderCancelled
                {
                    Id = message.Id,
                    DishId = cancelled.DishId,
                    OrderId = cancelled.OrderId,
                    CustomerNumber = cancelled.CustomerNumber,
                    OccurredAt = cancelled.OccurredAt
                },
                _ => throw new NotImplementedException()
            };
    }
}