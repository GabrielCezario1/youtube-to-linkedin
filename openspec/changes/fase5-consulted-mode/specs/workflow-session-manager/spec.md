## ADDED Requirements

### Requirement: Session registration
The system SHALL allow registering a paused workflow session identified by sessionId, storing a `TaskCompletionSource<string[]>`, the creation timestamp, and the post type.

#### Scenario: Successful registration
- **WHEN** `Register(sessionId, tcs, postType)` is called
- **THEN** the session is stored and retrievable by sessionId

### Requirement: Session response delivery
The system SHALL allow delivering answers to a registered session, completing the `TaskCompletionSource` and unblocking the waiting executor.

#### Scenario: Valid session respond
- **WHEN** `Respond(sessionId, answers)` is called and the session exists and has not timed out
- **THEN** `TCS.SetResult(answers)` is called and the method returns `true`

#### Scenario: Non-existent session respond
- **WHEN** `Respond(sessionId, answers)` is called and the sessionId is not registered
- **THEN** the method returns `false`

#### Scenario: Already-completed session respond
- **WHEN** `Respond(sessionId, answers)` is called but the TCS task is already completed
- **THEN** the method returns `false` without throwing

### Requirement: Session cleanup
The system SHALL remove a session from the dictionary after it has been resolved or expired, preventing memory leaks.

#### Scenario: Cleanup after respond
- **WHEN** `Cleanup(sessionId)` is called
- **THEN** the session is removed from the dictionary

### Requirement: Automatic session expiration
The system SHALL expire sessions that have been waiting for more than 10 minutes by cancelling the `TaskCompletionSource` with a `TimeoutException` and emitting a SignalR error event.

#### Scenario: Session expires after 10 minutes
- **WHEN** a registered session has `CreatedAt` more than 10 minutes in the past
- **THEN** the background task sets the TCS as cancelled (or faulted) and the executor catches it and emits `{ step: "writing", status: "error", message: "Sessão expirada. Inicie novamente." }`

#### Scenario: Active sessions are not expired
- **WHEN** a registered session has `CreatedAt` less than 10 minutes in the past
- **THEN** the background task leaves it untouched
