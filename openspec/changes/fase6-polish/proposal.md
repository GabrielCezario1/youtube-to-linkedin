## Why

As fases anteriores implementaram o happy path completo (transcrição, sumarização, geração automática e modo consultado), mas os cenários de erro não têm cobertura adequada — falhas de rede, vídeos inacessíveis, timeouts e cancelamentos deixam a UI em estado indefinido. Esta fase solidifica a robustez do produto sem adicionar novas funcionalidades.

## What Changes

- Validação de entrada no endpoint `/api/workflow/start` antes de disparar qualquer executor (URL, postType, mode)
- Propagação de `CancellationToken` pelos executors para suporte a cancelamento explícito pelo usuário
- Emissão de evento SignalR de erro tipificado (`transcript_inaccessible`, `no_transcript`, `llm_error`, `session_expired`) em vez de evento genérico
- Endpoint `DELETE /api/workflow/{sessionId}` para cancelamento iniciado pelo frontend
- Bloqueio completo do formulário Angular durante processamento (não apenas o botão)
- Botão "Cancelar" funcional em todas as etapas, incluindo pausa do modo Consultado
- Lógica de retry diferenciada no frontend: URL preservada para erros genéricos, URL limpa para "sem transcrição"
- Layout responsivo com breakpoints 375 px, 768 px e 1 280 px
- Reset completo limpa todos os estados sem recarregar a página

## Capabilities

### New Capabilities

- `input-validation`: Validação de URL, postType e mode no endpoint `/start` com respostas 400 tipificadas
- `cancellation`: Endpoint de cancelamento + propagação de `CancellationToken` pelos executors
- `error-events`: Eventos SignalR de erro com códigos semânticos para cada cenário de falha
- `frontend-error-handling`: Lógica de retry/reset no frontend por tipo de erro + bloqueio de formulário
- `responsive-layout`: CSS responsivo para os três breakpoints definidos no PRD

### Modified Capabilities

<!-- Nenhuma spec existente em openspec/specs/ — não há requisitos pré-existentes a atualizar -->

## Impact

- **Backend**: `WorkflowStartEndpoint.cs`, `WorkflowRespondEndpoint.cs` (novo endpoint de cancel), `TranscriptExecutor.cs`, `SummaryExecutor.cs`, `LinkedInWriterExecutor.cs`, `WorkflowSessionManager.cs`
- **Frontend**: `app.ts`, `app.html`, `app.css`, `signalr.service.ts`, `workflow.service.ts`, `workflow-progress.ts/.html`, `post-draft.ts/.html`
- **Sem novas dependências externas** — usa apenas primitivos já presentes (CancellationToken, HttpClient, Angular signals)
