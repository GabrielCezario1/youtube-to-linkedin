## ADDED Requirements

### Requirement: SummaryExecutor calls Azure OpenAI with the transcript
The system SHALL call Azure OpenAI (via `Azure.AI.OpenAI` v2.1.0) with a system prompt loaded from `Prompts/summarizer-system.md` and the raw transcript as the user message, returning a numbered list of 5–8 key technical points as a plain string.

#### Scenario: Successful summarisation
- **WHEN** a valid, non-empty transcript string is provided
- **THEN** the executor calls Azure OpenAI and returns a non-empty string containing a numbered list of 5–8 key points

#### Scenario: LLM API failure
- **WHEN** the Azure OpenAI API returns an error (e.g., `RequestFailedException`)
- **THEN** the executor emits a `workflowEvent` with `{ step: "summary", status: "error", message: "Ocorreu um erro ao processar o conteúdo. Tente novamente." }` and re-throws the exception

#### Scenario: Request timeout
- **WHEN** the Azure OpenAI call times out (`TaskCanceledException`)
- **THEN** the executor emits a `workflowEvent` with `{ step: "summary", status: "error", message: "Ocorreu um erro ao processar o conteúdo. Tente novamente." }` and re-throws the exception

### Requirement: SummaryExecutor emits SignalR progress events
The system SHALL emit `workflowEvent` SignalR events to the client session at the start and end of the summarisation step.

#### Scenario: Event emitted before LLM call
- **WHEN** `ExecuteAsync` is invoked
- **THEN** a `workflowEvent` with `{ step: "summary", status: "in_progress" }` is sent to the client session before the Azure OpenAI API is called

#### Scenario: Event emitted after successful LLM response
- **WHEN** Azure OpenAI returns a successful response
- **THEN** a `workflowEvent` with `{ step: "summary", status: "completed" }` is sent to the client session

### Requirement: SummaryExecutor is stateless and registered as Singleton
The `SummaryExecutor` class SHALL hold no per-request state. It SHALL be registered as `AddSingleton` in `Program.cs`.

#### Scenario: Concurrent requests do not interfere
- **WHEN** two workflow requests are processed concurrently
- **THEN** each request's SignalR events are sent only to its own `sessionId` and the results do not cross

### Requirement: SummaryExecutor is chained after TranscriptExecutor
The system SHALL call `SummaryExecutor.ExecuteAsync` immediately after `TranscriptExecutor.ExecuteAsync` returns successfully, passing the transcript string and `sessionId`.

#### Scenario: Successful chain
- **WHEN** `TranscriptExecutor` completes without error
- **THEN** `SummaryExecutor` is invoked with the returned transcript string

#### Scenario: TranscriptExecutor failure skips SummaryExecutor
- **WHEN** `TranscriptExecutor` throws an exception
- **THEN** `SummaryExecutor` is NOT called and the exception propagates

### Requirement: Azure OpenAI configuration via appsettings and secrets
The system SHALL read `AzureOpenAI:Endpoint` and `AzureOpenAI:ModelId` from `appsettings.json`, and `AzureOpenAI:ApiKey` from user secrets (development) or environment variable (production). The API key SHALL NOT appear in `appsettings.json`.

#### Scenario: Missing ApiKey at startup
- **WHEN** `AzureOpenAI:ApiKey` is absent from configuration
- **THEN** `AzureOpenAIClient` construction fails at startup with a clear configuration error (fail-fast)
