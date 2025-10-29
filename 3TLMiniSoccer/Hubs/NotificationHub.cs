using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Runtime.InteropServices;

namespace _3TLMiniSoccer.Hubs
{
    [Authorize(Roles = "Admin, Staff")]
    public class NotificationHub : Hub
    {
        public async Task JoinAdminGroup()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "AdminGroup");
        }

        public async Task LeaveAdminGroup()
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "AdminGroup");
        }

        public override async Task OnConnectedAsync()
        {
            await JoinAdminGroup();
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await LeaveAdminGroup();
            await base.OnDisconnectedAsync(exception);
        }
    }
}
