using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TodoApp.Api.Hubs;

[Authorize]
public sealed class TodoHub : Hub
{
    public async Task SendStatusAsync(string message)
    {
        await Clients.All.SendAsync("status", message);
    }
}