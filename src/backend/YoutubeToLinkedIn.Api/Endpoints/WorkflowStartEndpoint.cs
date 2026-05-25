using System.Text.RegularExpressions;
using YoutubeToLinkedIn.Api.Executors;
using YoutubeToLinkedIn.Api.Hubs;
using YoutubeToLinkedIn.Api.Services;

namespace YoutubeToLinkedIn.Api.Endpoints;

public static class WorkflowStartEndpoint
{
    private static readonly Regex VideoIdRegex = new(
        @"(?:youtube\.com/(?:watch\?(?:.*&)?v=|shorts/)|youtu\.be/)([a-zA-Z0-9_-]{11})",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly HashSet<string> ValidPostTypes =
        new(StringComparer.OrdinalIgnoreCase) { "storytelling", "lista-pratica", "opiniao-provocativa", "noticia" };

    private static readonly HashSet<string> ValidModes =
        new(StringComparer.OrdinalIgnoreCase) { "automatico", "consultado" };

    public static IResult Handle(
        StartWorkflowRequest request,
        TranscriptExecutor transcriptExecutor,
        SummaryExecutor summaryExecutor,
        LinkedInWriterExecutor linkedInWriterExecutor,
        WorkflowSessionManager sessionManager)
    {
        if (!VideoIdRegex.IsMatch(request.Url))
            return Results.BadRequest(new { error = "URL do YouTube inválida.", field = "url" });

        if (!ValidPostTypes.Contains(request.PostType))
            return Results.BadRequest(new { error = "Tipo de post inválido.", field = "postType" });

        if (!ValidModes.Contains(request.Mode))
            return Results.BadRequest(new { error = "Modo inválido.", field = "mode" });

        var sessionId = Guid.NewGuid().ToString();
        var cts = new CancellationTokenSource();
        sessionManager.Register(sessionId, cts, request.PostType);

        _ = Task.Run(async () =>
        {
            try
            {
                var ct = cts.Token;
                var transcript = await transcriptExecutor.ExecuteAsync(request.Url, sessionId, ct);
                var summary = await summaryExecutor.ExecuteAsync(transcript, request.PostType, sessionId, ct);
                await linkedInWriterExecutor.ExecuteAsync(summary, request.PostType, sessionId, request.Mode, ct);
            }
            catch (OperationCanceledException)
            {
                sessionManager.Cleanup(sessionId);
            }
            catch
            {
                sessionManager.Cleanup(sessionId);
            }
        });

        return Results.Ok(new { sessionId });
    }
}

public record StartWorkflowRequest(string Url, string PostType, string Mode);

