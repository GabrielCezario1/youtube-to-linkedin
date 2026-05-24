using Microsoft.AspNetCore.SignalR;

namespace YoutubeToLinkedIn.Api.Hubs;

public class WorkflowHub : Hub
{
    public async Task SendProgress(string sessionId, string message)
    {
        await Clients.All.SendAsync("SendProgress", sessionId, message);
    }
}
