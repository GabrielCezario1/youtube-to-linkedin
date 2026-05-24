## ADDED Requirements

### Requirement: Respond endpoint accepts answers
The system SHALL expose `POST /api/workflow/{sessionId}/respond` accepting a JSON body `{ "answers": string[] }` and delivering the answers to the waiting executor session.

#### Scenario: Valid session respond returns 200
- **WHEN** `POST /api/workflow/{sessionId}/respond` is called with a valid sessionId and `answers` array
- **THEN** the response is `200 OK` and the executor is unblocked with the provided answers

#### Scenario: Unknown sessionId returns 404
- **WHEN** `POST /api/workflow/{sessionId}/respond` is called with a sessionId not registered in `WorkflowSessionManager`
- **THEN** the response is `404 Not Found`

#### Scenario: Already-expired session returns 404
- **WHEN** `POST /api/workflow/{sessionId}/respond` is called but the session has already been cleaned up or timed out
- **THEN** the response is `404 Not Found`

#### Scenario: Empty answers array is accepted
- **WHEN** `POST /api/workflow/{sessionId}/respond` is called with `answers: []`
- **THEN** the response is `200 OK` (blank answers are allowed; filtering happens in the executor)
