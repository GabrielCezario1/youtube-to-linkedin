using YoutubeToLinkedIn.Api.Executors;
using YoutubeToLinkedIn.Api.Hubs;

namespace YoutubeToLinkedIn.Api.Endpoints;

public static class WorkflowStartEndpoint
{
    public static IResult Handle(
        StartWorkflowRequest request,
        TranscriptExecutor transcriptExecutor,
        SummaryExecutor summaryExecutor)
    {
        var sessionId = Guid.NewGuid().ToString();

        _ = Task.Run(async () =>
        {
            try
            {
                var transcript = await transcriptExecutor.ExecuteAsync(request.Url, sessionId);
                await summaryExecutor.ExecuteAsync(transcript, sessionId);
            }
            catch
            {
                // Error already signaled to client via SignalR
            }
        });

        return Results.Ok(new { sessionId });
    }
}

public record StartWorkflowRequest(string Url, string PostType, string Mode);
