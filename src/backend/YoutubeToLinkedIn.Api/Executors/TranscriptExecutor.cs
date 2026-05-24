using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;
using YoutubeExplode;
using YoutubeExplode.Exceptions;
using YoutubeToLinkedIn.Api.Hubs;

namespace YoutubeToLinkedIn.Api.Executors;

public class TranscriptExecutor
{
    private static readonly Regex VideoIdRegex = new(
        @"(?:youtube\.com/(?:watch\?(?:.*&)?v=|shorts/)|youtu\.be/)([a-zA-Z0-9_-]{11})",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly IHubContext<WorkflowHub> _hubContext;

    public TranscriptExecutor(IHubContext<WorkflowHub> hubContext)
    {
        _hubContext = hubContext;
    }

    private static string? ExtractVideoId(string url)
    {
        var match = VideoIdRegex.Match(url);
        return match.Success ? match.Groups[1].Value : null;
    }

    public async Task<string> ExecuteAsync(string url, string sessionId, CancellationToken cancellationToken = default)
    {
        var videoId = ExtractVideoId(url);
        if (videoId is null)
        {
            await SendWorkflowEvent(sessionId, "error",
                "URL do YouTube inválida. Use um link no formato youtube.com/watch?v=... ou youtu.be/...",
                "invalid_url");
            throw new ArgumentException("Invalid YouTube URL", nameof(url));
        }

        await SendWorkflowEvent(sessionId, "in_progress");

        bool errorHandled = false;
        try
        {
            var youtube = new YoutubeClient();
            var manifest = await youtube.Videos.ClosedCaptions.GetManifestAsync(videoId, cancellationToken);

            if (!manifest.Tracks.Any())
            {
                errorHandled = true;
                await SendWorkflowEvent(sessionId, "error",
                    "Este vídeo não possui transcrição disponível. Tente com outro vídeo.",
                    "no_transcript");
                throw new InvalidOperationException("No captions available");
            }

            var trackInfo = manifest.Tracks.First();
            var track = await youtube.Videos.ClosedCaptions.GetAsync(trackInfo, cancellationToken);
            var transcript = string.Join(" ", track.Captions.Select(c => c.Text));

            await SendWorkflowEvent(sessionId, "completed");
            return transcript;
        }
        catch (OperationCanceledException) when (!errorHandled)
        {
            await SendWorkflowEvent(sessionId, "error", "Workflow cancelado.", "cancelled");
            throw;
        }
        catch (VideoUnavailableException) when (!errorHandled)
        {
            await SendWorkflowEvent(sessionId, "error",
                "Não foi possível acessar este vídeo. Verifique se ele é público e tente novamente.",
                "video_inaccessible");
            throw;
        }
        catch (Exception) when (!errorHandled)
        {
            await SendWorkflowEvent(sessionId, "error",
                "Ocorreu um erro ao extrair a transcrição. Tente novamente.",
                "llm_error");
            throw;
        }
    }

    private Task SendWorkflowEvent(string sessionId, string status, string? message = null, string? errorCode = null)
    {
        object payload;
        if (errorCode is not null && message is not null)
            payload = new { step = "transcript", status, errorCode, message };
        else if (message is not null)
            payload = new { step = "transcript", status, message };
        else
            payload = new { step = "transcript", status };

        return _hubContext.Clients.All.SendAsync("workflowEvent", sessionId, payload);
    }
}
