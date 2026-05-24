namespace YoutubeToLinkedIn.Api.Models;

public sealed class ActiveSession
{
    public required string SessionId { get; init; }
    public required TaskCompletionSource<string[]> Tcs { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required string PostType { get; init; }
}
