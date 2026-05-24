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

    public void Register(string sessionId, CancellationTokenSource cts, string postType)
    {
        var session = new ActiveSession
        {
            SessionId = sessionId,
            Cts = cts,
            CreatedAt = DateTime.UtcNow,
            PostType = postType
        };
        _sessions[sessionId] = session;
    }

    public void AttachTcs(string sessionId, TaskCompletionSource<string[]> tcs)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
            session.Tcs = tcs;
    }

    public bool Respond(string sessionId, string[] answers)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
            return false;

        if (session.Tcs is null || session.Tcs.Task.IsCompleted)
            return false;

        return session.Tcs.TrySetResult(answers);
    }

    public bool Cancel(string sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
            return false;

        session.Cts.Cancel();
        return true;
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
            // Remove sessions already explicitly cancelled
            if (session.Cts.IsCancellationRequested)
            {
                _sessions.TryRemove(sessionId, out _);
                continue;
            }

            if (session.CreatedAt >= cutoff) continue;

            if (session.Tcs is not null && !session.Tcs.Task.IsCompleted)
            {
                // Consulted session waiting for user input — expire it
                if (session.Tcs.TrySetCanceled())
                {
                    _sessions.TryRemove(sessionId, out _);

                    var payload = new { step = "writing", status = "error", errorCode = "session_expired", message = "Sessão expirada. Inicie novamente." };
                    _ = _hubContext.Clients.All.SendAsync("workflowEvent", sessionId, payload);
                }
            }
            else if (session.Tcs is null)
            {
                // Auto session — remove silently
                _sessions.TryRemove(sessionId, out _);
            }
        }
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
