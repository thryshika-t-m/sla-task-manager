using Microsoft.AspNetCore.SignalR;
using SlaTaskManager.API.Models.Dtos;

namespace SlaTaskManager.API.Hubs;

public class TaskHub : Hub
{
    public async Task BroadcastTaskUpdate(TaskResponse taskUpdate)
    {
        await Clients.All.SendAsync("BroadcastTaskUpdate", taskUpdate);
    }
}
