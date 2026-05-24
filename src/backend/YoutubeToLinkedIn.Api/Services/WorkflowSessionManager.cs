using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using YoutubeToLinkedIn.Api.Hubs;
using YoutubeToLinkedIn.Api.Models;

namespace YoutubeToLinkedIn.Api.Services;

public sealed class WorkflowSessionManager : IHostedService, IDisposable
{
    private readonly ConcurrentDictionary<string, ActiveSession> _sessions = new();
    private readonly IHubContext<WorkflowHub> _hubContext;
    private Timer? _timer;

    private static readonly TimeSpan SessionTimeout = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan SweepInterval = TimeSpan.FromSeconds(60);

    public WorkflowSessionManager(IHubContext<WorkflowHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public void Register(string sessionId, TaskCompletionSource<string[]> tcs, string postType)
    {
        var session = new ActiveSession
        {
            SessionId = sessionId,
            Tcs = tcs,
            CreatedAt = DateTime.UtcNow,
            PostType = postType
        };
        _sessions[sessionId] = session;
    }

    public bool Respond(string sessionId, string[] answers)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
            return false;

        if (session.Tcs.Task.IsCompleted)
            return false;

        return session.Tcs.TrySetResult(answers);
    }

    public void Cleanup(string sessionId)
    {
        _sessions.TryRemove(sessionId, out _);
    }

    // IHostedService
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(ExpireStale, null, SweepInterval, SweepInterval);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    private void ExpireStale(object? state)
    {
        var cutoff = DateTime.UtcNow - SessionTimeout;
        foreach (var (sessionId, session) in _sessions)
        {
            if (session.CreatedAt < cutoff && !session.Tcs.Task.IsCompleted)
            {
                if (session.Tcs.TrySetCanceled())
                {
                    _sessions.TryRemove(sessionId, out _);

                    // Notify client of expiration
                    var payload = new { step = "writing", status = "error", message = "Sessão expirada. Inicie novamente." };
                    _ = _hubContext.Clients.All.SendAsync("workflowEvent", sessionId, payload);
                }
            }
        }
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
