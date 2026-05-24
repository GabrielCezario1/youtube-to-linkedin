using YoutubeToLinkedIn.Api.Services;

namespace YoutubeToLinkedIn.Api.Endpoints;

public static class WorkflowCancelEndpoint
{
    public static IResult Handle(string sessionId, WorkflowSessionManager sessionManager)
    {
        var cancelled = sessionManager.Cancel(sessionId);
        return cancelled ? Results.Ok() : Results.NotFound();
    }
}
