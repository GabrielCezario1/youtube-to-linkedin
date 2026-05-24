using Microsoft.AspNetCore.SignalR;
using YoutubeToLinkedIn.Api.Hubs;

namespace YoutubeToLinkedIn.Api.Endpoints;

public static class WorkflowStartEndpoint
{
    public static async Task<IResult> Handle(
        StartWorkflowRequest request,
        IHubContext<WorkflowHub> hubContext)
    {
        var sessionId = Guid.NewGuid().ToString();

        await hubContext.Clients.All.SendAsync("SendProgress", sessionId, "Workflow started (mock)");

        return Results.Ok(new { sessionId });
    }
}

public record StartWorkflowRequest(string Url, string PostType, string Mode);
