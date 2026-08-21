using Microsoft.AspNetCore.SignalR;

namespace CRM.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time user presence, online/offline status, and activity synchronization.
    /// </summary>
    public class UserStatusHub : Hub
    {
        public const string HubPath = "/hubs/user-status";

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}
