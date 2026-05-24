using YoutubeToLinkedIn.Api.Services;

namespace YoutubeToLinkedIn.Api.Endpoints;

public static class WorkflowRespondEndpoint
{
    public static IResult Handle(
        string sessionId,
        RespondWorkflowRequest request,
        WorkflowSessionManager sessionManager)
    {
        var delivered = sessionManager.Respond(sessionId, request.Answers);
        if (!delivered)
            return Results.NotFound();

        sessionManager.Cleanup(sessionId);
        return Results.Ok();
    }
}

public record RespondWorkflowRequest(string[] Answers);
