using System.Threading;
using System.Threading.Tasks;
using BurgerJoint.StoreFront.Data;
using Nito.AsyncEx;

namespace BurgerJoint.StoreFront.Infrastructure
{
    public class OutboxMessageListener : IOutboxMessageListener
    {
        private readonly AsyncAutoResetEvent _autoResetEvent = new();

        public void OnNewMessages() => _autoResetEvent.Set();

        public Task WaitForMessagesAsync(CancellationToken ct) => _autoResetEvent.WaitAsync(ct);
    }
}