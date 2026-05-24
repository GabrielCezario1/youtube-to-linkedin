## Why

The pipeline currently extracts the raw transcript from a YouTube video but has no way to distil that text into structured, actionable insights. Without a summarisation step, the downstream LinkedIn-post writer would have to process hundreds of lines of raw speech, producing lower-quality output and making the workflow brittle for long videos. Introducing `SummaryExecutor` closes this gap and completes the data-transformation chain: transcript → key points → post.

## What Changes

- Add a `SummaryExecutor` that calls Azure OpenAI (via the `Azure.AI.OpenAI` v2.1.0 SDK) with the raw transcript and returns a numbered list of 5–8 key technical points.
- Add a `PromptLoader` singleton that reads all `*.md` files from `Prompts/` on startup and exposes them via `GetPrompt(string name)`, eliminating per-request file I/O.
- Add `Prompts/summarizer-system.md` with the system prompt (instruction, output format, and constraints).
- Wire `SummaryExecutor` into `WorkflowStartEndpoint` immediately after `TranscriptExecutor`.
- Migrate `TranscriptExecutor` from `AddScoped` to `AddSingleton` in `Program.cs` (both executors are stateless).
- Register `AzureOpenAIClient` as a singleton in `Program.cs`.
- Emit SignalR `workflowEvent` events (`in_progress`, `completed`, `error`) for the `"summary"` step.
- Surface configuration keys `AzureOpenAI:Endpoint` and `AzureOpenAI:ModelId` in `appsettings.json`; `AzureOpenAI:ApiKey` stored in user secrets (dev) / environment variable (prod).

## Capabilities

### New Capabilities

- `summary-execution`: Calls Azure OpenAI with the raw transcript and returns a structured numbered list of 5–8 key technical points, emitting SignalR progress events throughout.
- `prompt-loader`: Singleton service that loads all `*.md` files from the `Prompts/` directory at application startup and exposes them by name, avoiding repeated disk I/O.

### Modified Capabilities

<!-- No existing spec-level requirement changes in this phase. -->

## Impact

- **Backend**: New files — `Executors/SummaryExecutor.cs`, `Services/PromptLoader.cs`, `Prompts/summarizer-system.md`; modified — `Program.cs` (DI registrations, configuration), `Endpoints/WorkflowStartEndpoint.cs` (chain SummaryExecutor after TranscriptExecutor).
- **Configuration**: `appsettings.json` gains `AzureOpenAI:Endpoint` and `AzureOpenAI:ModelId`; `AzureOpenAI:ApiKey` via `dotnet user-secrets` / env var.
- **Dependencies**: Add NuGet package `Azure.AI.OpenAI` v2.1.0.
- **Frontend**: No UI changes required — the `WorkflowProgressComponent` already handles the `"summary"` step (StepId and steps array updated in Fase 2).
- **SignalR contract**: New step identifier `"summary"` added to the event stream (non-breaking; frontend already expects it).
