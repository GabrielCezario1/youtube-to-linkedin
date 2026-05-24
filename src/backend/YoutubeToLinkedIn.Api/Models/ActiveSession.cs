namespace YoutubeToLinkedIn.Api.Models;

public sealed class ActiveSession
{
    public required string SessionId { get; init; }
    public TaskCompletionSource<string[]>? Tcs { get; set; }
    public required CancellationTokenSource Cts { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required string PostType { get; init; }
}
