using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace BurgerJoint.Operations.Features.Dashboard
{
    public record EventInfo(DateTime OccurredAt, string Detail);
    
    public interface IDashboardHub
    {
        Task ReceiveEvent(EventInfo eventInfo);
    }
    
    public class DashboardHub : Hub<IDashboardHub>
    {
    }
}