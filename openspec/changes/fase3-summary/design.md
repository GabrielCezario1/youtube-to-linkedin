## Context

The backend is an ASP.NET Core 10 minimal-API application. `TranscriptExecutor` (currently `AddScoped`) extracts the raw YouTube transcript and emits SignalR events. `WorkflowStartEndpoint` chains executors inside a fire-and-forget `Task.Run`. There is no LLM integration yet and no shared infrastructure for loading prompt files.

This phase introduces `SummaryExecutor`, which calls Azure OpenAI (`Azure.AI.OpenAI` v2.1.0) to distil the raw transcript into a structured list of 5–8 key technical points. It also introduces `PromptLoader`, a singleton that reads all `Prompts/*.md` files at startup.

## Goals / Non-Goals

**Goals:**
- Introduce `PromptLoader` singleton that loads `Prompts/*.md` files once at startup.
- Introduce `SummaryExecutor` singleton that calls Azure OpenAI and emits `workflowEvent` SignalR events for the `"summary"` step.
- Wire `SummaryExecutor` after `TranscriptExecutor` in `WorkflowStartEndpoint`.
- Migrate `TranscriptExecutor` from `AddScoped` to `AddSingleton`.
- Expose `AzureOpenAI:Endpoint` and `AzureOpenAI:ModelId` in `appsettings.json`; `ApiKey` via user secrets / env var.

**Non-Goals:**
- Displaying the summary in the UI.
- Any mode-awareness (Auto / Consultado) inside `SummaryExecutor`.
- Streaming responses from Azure OpenAI.
- Prompt caching or versioning beyond file-based loading.
- Agent Framework or Semantic Kernel abstractions.

## Decisions

### D1 — SDK: `Azure.AI.OpenAI` v2.1.0 directly (no Semantic Kernel / Agent Framework)
The PRD mandates the Azure OpenAI SDK without abstractions. Direct SDK usage keeps the dependency surface minimal and avoids premature framework adoption. Semantic Kernel would be natural for later phases but is out of scope here.

### D2 — `PromptLoader` as a Singleton with startup loading
Prompt files are static assets that change only on redeployment. Loading them at startup (via `IHostedService`-style init in the constructor, called during `AddSingleton` resolution) eliminates per-request I/O. The loader scans `Prompts/` relative to `ContentRootPath` and stores prompts in an in-memory dictionary keyed by file name (without extension).

*Alternative considered*: Lazy per-request file read — rejected because it adds latency to every LLM call and pollutes the hot path.

### D3 — `SummaryExecutor` and `TranscriptExecutor` registered as Singleton
Both executors are stateless (no per-request fields). Registering them as `AddSingleton` is semantically correct and avoids the `ObjectDisposedException` risk that arises when a Scoped service is captured inside `Task.Run` (the scope can be disposed before the background task completes).

*Alternative considered*: Keep `TranscriptExecutor` as Scoped — rejected because `IHubContext<T>` is safe to use from singletons and the executor holds no request-scoped state.

### D4 — `AzureOpenAIClient` registered as Singleton
`AzureOpenAIClient` is thread-safe by design (documented by the Azure SDK team). A single instance is shared across all requests to reuse connection pools.

### D5 — Error messages: always generic to the client
`RequestFailedException` and other Azure SDK exceptions are caught, a generic user-facing message is emitted via SignalR, and the original exception is re-thrown for server-side logging. Internal API details (endpoint, model name, quota messages) are never surfaced to the frontend.

### D6 — Chaining executors inside `WorkflowStartEndpoint` (no WorkflowFactory)
The PRD explicitly states no `WorkflowFactory` in this phase. `WorkflowStartEndpoint.Handle` is a static method that receives both executors via parameter injection, calls them sequentially with `await`, and catches exceptions individually. Each executor is responsible for its own SignalR error event; the endpoint's catch block is a safety net.

## Risks / Trade-offs

| Risk | Mitigation |
|---|---|
| Long transcripts may exceed the model's context window | Log token usage in dev; monitor in prod. No truncation in MVP — context window of gpt-4o-mini is 128k tokens, sufficient for most videos. |
| Azure OpenAI cold-start latency on first request | Singleton client reuses HTTP connections; no additional mitigation needed in MVP. |
| `AzureOpenAI:ApiKey` committed to source control | Key stored in `dotnet user-secrets` (dev) and environment variable (prod). `appsettings.json` contains only non-secret keys. |
| `Prompts/` directory missing at startup | `PromptLoader` throws a clear `DirectoryNotFoundException` at startup (fail-fast), preventing silent failures at runtime. |
| Future executors needing `PromptLoader` | Singleton registration makes it injectable in any executor without changes. |

## Migration Plan

1. Add `Azure.AI.OpenAI` NuGet package.
2. Create `Prompts/summarizer-system.md`.
3. Create `Services/PromptLoader.cs` and register as Singleton.
4. Create `Executors/SummaryExecutor.cs` and register as Singleton.
5. Change `TranscriptExecutor` registration from `AddScoped` to `AddSingleton`.
6. Register `AzureOpenAIClient` as Singleton in `Program.cs`.
7. Add `AzureOpenAI:Endpoint` and `AzureOpenAI:ModelId` to `appsettings.json`.
8. Update `WorkflowStartEndpoint` to inject and chain `SummaryExecutor`.
9. Set `AzureOpenAI:ApiKey` via `dotnet user-secrets set`.

**Rollback**: Remove `SummaryExecutor` injection from the endpoint and comment out the chained `await` call — transcript extraction continues to work independently.

## Open Questions

- Should `PromptLoader` watch for file changes (hot-reload) in development? Deferred to a future improvement; not required for MVP.
- What is the target model — `gpt-4o` or `gpt-4o-mini`? Decision: default to `gpt-4o-mini` (configurable via `AzureOpenAI:ModelId`).
