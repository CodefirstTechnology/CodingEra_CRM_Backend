using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CRM.DATA;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CRM.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time user presence, online/offline status, 
    /// persistent follow-up popup reminders, and admin SLA escalation notifications.
    /// </summary>
    public class UserStatusHub : Hub
    {
        public const string HubPath = "/hubs/user-status";
        public const string AdminsGroupName = "AdminsGroup";

        private readonly TaskDbcontext _context;

        public UserStatusHub(TaskDbcontext context)
        {
            _context = context;
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();

            var httpContext = Context.GetHttpContext();
            var userIdStr = httpContext?.Request.Query["userId"].ToString()
                ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? Context.User?.FindFirst("sub")?.Value;

            if (int.TryParse(userIdStr, out var userId) && userId > 0)
            {
                // Join user-specific group for targeted notifications
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");

                // Resolve user role to determine if user belongs to Admins/Managers group
                var user = await _context.Users.AsNoTracking()
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user?.Role != null)
                {
                    var roleName = (user.Role.Name ?? "").Trim().ToLower();
                    if (roleName.Contains("admin") || roleName.Contains("manager"))
                    {
                        await Groups.AddToGroupAsync(Context.ConnectionId, AdminsGroupName);
                    }
                }
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}
