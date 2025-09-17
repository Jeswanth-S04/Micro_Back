using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BudgetManagementSystem.Api.Hubs
{
    [Authorize]
    public class BudgetHub : Hub
    {
        public override Task OnConnectedAsync()
        {
            // Frontend can add to groups by department or role via Send/Invoke if needed
            return base.OnConnectedAsync();
        }
    }
}
