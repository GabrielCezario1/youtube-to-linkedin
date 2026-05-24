## MODIFIED Requirements

### Requirement: Step status representation
The `WorkflowProgressComponent` SHALL display a distinct visual indicator for each step status, including the new `awaiting_input` state which represents a step that is paused waiting for user input.

- **WHEN** a step has `status: 'awaiting_input'`
- **THEN** the step displays a pause/waiting icon (e.g. ⏸) and a "Aguardando sua resposta..." label, distinct from `in_progress` (spinner), `completed` (✓), and `error` (✗)

#### Scenario: Awaiting input visual state
- **WHEN** the writing step emits `status: 'awaiting_input'`
- **THEN** the workflow progress indicator shows the writing step as paused with the pause icon

#### Scenario: In progress after user responds
- **WHEN** the writing step emits `status: 'in_progress'` after the user submits answers
- **THEN** the workflow progress indicator returns to showing the spinner for the writing step
