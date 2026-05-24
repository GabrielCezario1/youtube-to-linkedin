## ADDED Requirements

### Requirement: Mode-based execution branching
The `LinkedInWriterExecutor` SHALL accept a `mode` parameter and execute the Automatic path when `mode == "automatico"` and the Consulted path when `mode == "consultado"`.

#### Scenario: Automatic mode executes unchanged
- **WHEN** `ExecuteAsync(summary, postType, sessionId, mode: "automatico")` is called
- **THEN** the executor runs the Fase 4 flow with no pauses

#### Scenario: Consulted mode executes new path
- **WHEN** `ExecuteAsync(summary, postType, sessionId, mode: "consultado")` is called
- **THEN** the executor runs the human-in-the-loop path with questions, pause, and enriched prompt

### Requirement: Fixed questions per template
The executor SHALL generate a fixed set of questions based on the `postType` parameter before pausing.

#### Scenario: Storytelling questions
- **WHEN** `postType == "storytelling"`
- **THEN** questions include "Qual foi o erro ou obstáculo principal?", "Qual foi o aprendizado mais valioso?", "Para quem é este post?"

#### Scenario: Lista Prática questions
- **WHEN** `postType == "lista-pratica"`
- **THEN** questions include "Algum item da lista tem contexto da sua experiência?", "Para quem é este post?"

#### Scenario: Opinião Provocativa questions
- **WHEN** `postType == "opiniao-provocativa"`
- **THEN** questions include "Qual é a crença comum que você quer questionar?", "Você tem um dado ou exemplo concreto para reforçar?"

### Requirement: Dynamic questions via LLM
The executor SHALL call the LLM to generate up to 3 additional context-specific questions based on the summary.

#### Scenario: Dynamic questions generated successfully
- **WHEN** the LLM returns 1-3 valid question strings in JSON array format
- **THEN** those questions are appended to the fixed questions list

#### Scenario: LLM fails to generate dynamic questions
- **WHEN** the LLM call for dynamic questions throws an exception or returns invalid JSON
- **THEN** the executor silently catches the error and continues with only the fixed questions

### Requirement: Await user input via session manager
The executor SHALL emit `{ step: "writing", status: "awaiting_input", questions: [...] }` via SignalR, register the session with `WorkflowSessionManager`, and await the TCS to receive user answers.

#### Scenario: Pause emits awaiting_input event
- **WHEN** the executor reaches the pause point
- **THEN** `WorkflowHub` receives `workflowEvent(sessionId, { step: "writing", status: "awaiting_input", questions: [...] })`

#### Scenario: Session registered before await
- **WHEN** the pause point is reached
- **THEN** `WorkflowSessionManager.Register(sessionId, tcs, postType)` is called before `await tcs.Task`

### Requirement: Enriched prompt with user answers
After receiving answers, the executor SHALL build the user message as `"Tipo de post: {postType}\n\nResumo:\n{summary}\n\nContexto adicional do autor:\n{filteredAnswers}"` where blank answers are filtered out.

#### Scenario: Non-blank answers are included
- **WHEN** user provides non-empty answers
- **THEN** those answers are appended as key-value lines in the user message

#### Scenario: Blank answers are filtered
- **WHEN** user provides one or more empty or whitespace-only answers
- **THEN** those answers are excluded from the prompt; remaining answers use 1-indexed labels ("1. ...", "2. ...")

### Requirement: Session timeout handling
The executor SHALL catch `OperationCanceledException` from the awaited TCS and emit a SignalR error event with message "Sessão expirada. Inicie novamente." before returning.

#### Scenario: Timeout triggers error event
- **WHEN** the TCS task is cancelled due to expiration
- **THEN** `workflowEvent(sessionId, { step: "writing", status: "error", message: "Sessão expirada. Inicie novamente." })` is emitted
