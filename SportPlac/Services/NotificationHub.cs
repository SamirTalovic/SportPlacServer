using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;

namespace SportPlac.Services
{
    public class NotificationHub : Hub
    {
        public async Task JoinUserGroup(string userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        }
    }
}
