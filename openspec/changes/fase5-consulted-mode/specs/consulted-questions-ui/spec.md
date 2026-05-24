## ADDED Requirements

### Requirement: Render contextual questions
The `ConsultedQuestionsComponent` SHALL receive a list of questions and render one labeled textarea per question.

#### Scenario: Questions are displayed
- **WHEN** `[questions]` input is provided with N strings
- **THEN** N textareas are rendered, each labeled with the question text

#### Scenario: All questions are optional
- **WHEN** the component is displayed
- **THEN** a note "Todas as perguntas são opcionais" is shown near the submit button

### Requirement: Submit answers to backend
The component SHALL collect the current value of each textarea and call `WorkflowService.respond(sessionId, answers)` when the "Continuar" button is clicked.

#### Scenario: Continuar triggers respond call
- **WHEN** user clicks "Continuar"
- **THEN** `WorkflowService.respond(currentSessionId, answers)` is called with one string per textarea (empty strings allowed)

### Requirement: Hide after submission
The component SHALL hide itself (or be removed from DOM) after "Continuar" is clicked and the request is in progress.

#### Scenario: Component hidden after submit
- **WHEN** "Continuar" is clicked
- **THEN** the component emits a `(submitted)` output event and the parent removes it from the DOM

### Requirement: WorkflowService respond method
The `WorkflowService` (or `SignalRService`) SHALL expose a `respond(sessionId: string, answers: string[]): Observable<void>` method that calls `POST /api/workflow/{sessionId}/respond`.

#### Scenario: Successful respond call
- **WHEN** `respond(sessionId, answers)` is called
- **THEN** it issues `POST /api/workflow/{sessionId}/respond` with body `{ answers }` and returns an Observable that completes on 200
