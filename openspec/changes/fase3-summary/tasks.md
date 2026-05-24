## 1. Dependencies & Configuration

- [x] 1.1 Add NuGet package `Azure.AI.OpenAI` v2.1.0 to `YoutubeToLinkedIn.Api.csproj`
- [x] 1.2 Add `AzureOpenAI:Endpoint` and `AzureOpenAI:ModelId` keys to `appsettings.json` (no API key)
- [x] 1.3 Set `AzureOpenAI:ApiKey` via `dotnet user-secrets set "AzureOpenAI:ApiKey" "<value>"`

## 2. Prompt File

- [x] 2.1 Create `Prompts/summarizer-system.md` with the system instruction, required output format (numbered list: title + context line), and constraints (technical focus, ignore intros/sponsors, no invented content, respond with list only)

## 3. PromptLoader Service

- [x] 3.1 Create `Services/PromptLoader.cs`: constructor scans `Prompts/` relative to `IWebHostEnvironment.ContentRootPath`, reads all `*.md` files into a case-insensitive `Dictionary<string, string>` keyed by file name without extension; throws `DirectoryNotFoundException` if directory is absent
- [x] 3.2 Implement `GetPrompt(string name)` method: looks up the dictionary; throws `KeyNotFoundException` with descriptive message if not found
- [x] 3.3 Register `PromptLoader` as `AddSingleton` in `Program.cs`

## 4. AzureOpenAIClient Registration

- [x] 4.1 Register `AzureOpenAIClient` as `AddSingleton` in `Program.cs`, reading `AzureOpenAI:Endpoint` and `AzureOpenAI:ApiKey` from `IConfiguration`

## 5. SummaryExecutor

- [x] 5.1 Create `Executors/SummaryExecutor.cs` with constructor injecting `PromptLoader`, `IHubContext<WorkflowHub>`, and `AzureOpenAIClient`
- [x] 5.2 Implement `ExecuteAsync(string transcript, string sessionId)`: emit `in_progress` → build chat messages (system from `PromptLoader.GetPrompt("summarizer-system")`, user = transcript) → call `AzureOpenAIClient` with `Temperature = 0.3f` and `MaxOutputTokenCount = 1200` → return response content string → emit `completed`
- [x] 5.3 Add exception handling: catch `RequestFailedException`, `TaskCanceledException`, and `Exception`; emit `error` SignalR event with generic user message; re-throw
- [x] 5.4 Register `SummaryExecutor` as `AddSingleton` in `Program.cs`

## 6. TranscriptExecutor Migration

- [x] 6.1 Change `TranscriptExecutor` registration from `AddScoped` to `AddSingleton` in `Program.cs`

## 7. WorkflowStartEndpoint Wiring

- [x] 7.1 Add `SummaryExecutor summaryExecutor` parameter to `WorkflowStartEndpoint.Handle`
- [x] 7.2 Chain `await summaryExecutor.ExecuteAsync(transcript, sessionId)` immediately after `TranscriptExecutor` returns the transcript string inside the `Task.Run` block
