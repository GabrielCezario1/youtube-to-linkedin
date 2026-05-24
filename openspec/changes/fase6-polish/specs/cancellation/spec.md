## ADDED Requirements

### Requirement: Sessão é registrada com CancellationTokenSource no início do workflow
O `WorkflowSessionManager` SHALL registrar um `CancellationTokenSource` por `sessionId` no momento em que o workflow é iniciado em `/api/workflow/start`, antes de disparar o `Task.Run`.

#### Scenario: Sessão registrada ao iniciar workflow
- **WHEN** `POST /api/workflow/start` retorna `200 OK`
- **THEN** `WorkflowSessionManager` possui uma entrada com o `sessionId` retornado
- **THEN** a entrada contém um `CancellationTokenSource` ativo (não cancelado)

### Requirement: Endpoint de cancelamento interrompe o workflow em curso
O sistema SHALL expor `DELETE /api/workflow/{sessionId}` que cancela o `CancellationToken` da sessão correspondente.

#### Scenario: Cancelamento de sessão existente retorna 200
- **WHEN** o cliente envia `DELETE /api/workflow/{sessionId}` para uma sessão ativa
- **THEN** o servidor retorna `200 OK`
- **THEN** o `CancellationToken` da sessão é marcado como cancelado

#### Scenario: Cancelamento de sessão inexistente retorna 404
- **WHEN** o cliente envia `DELETE /api/workflow/{sessionId}` para um sessionId desconhecido
- **THEN** o servidor retorna `404 Not Found`

### Requirement: CancellationToken é propagado pelos executors
Cada executor (`TranscriptExecutor`, `SummaryExecutor`, `LinkedInWriterExecutor`) SHALL aceitar um `CancellationToken` e repassá-lo para todas as chamadas async externas (YoutubeExplode, Azure OpenAI).

#### Scenario: Cancelamento durante extração de transcrição
- **WHEN** o CancellationToken é cancelado enquanto `TranscriptExecutor` aguarda YoutubeExplode
- **THEN** a operação lança `OperationCanceledException`
- **THEN** o executor emite evento SignalR `{ step, status: "error", errorCode: "cancelled", message: "Workflow cancelado." }`

#### Scenario: Cancelamento durante chamada ao LLM
- **WHEN** o CancellationToken é cancelado enquanto `SummaryExecutor` ou `LinkedInWriterExecutor` aguarda a resposta do OpenAI
- **THEN** a operação lança `OperationCanceledException`
- **THEN** o executor emite evento SignalR `{ step, status: "error", errorCode: "cancelled", message: "Workflow cancelado." }`

### Requirement: Sessão é removida do manager após conclusão ou cancelamento
O `WorkflowSessionManager` SHALL remover a entrada de sessão após o workflow concluir (com sucesso, erro ou cancelamento), liberando recursos.

#### Scenario: Remoção após conclusão com sucesso
- **WHEN** o `LinkedInWriterExecutor` emite `status: "completed"`
- **THEN** `WorkflowSessionManager.Cleanup(sessionId)` é chamado
- **THEN** a sessão não existe mais no manager

#### Scenario: Remoção após cancelamento
- **WHEN** `OperationCanceledException` é capturada no `Task.Run` de `WorkflowStartEndpoint`
- **THEN** `WorkflowSessionManager.Cleanup(sessionId)` é chamado
