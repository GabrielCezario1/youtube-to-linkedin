## 1. Modelo e Session Manager (backend)

- [ ] 1.1 Adicionar `CancellationTokenSource Cts { get; init; }` a `ActiveSession.cs` e tornar `Tcs` nullable (`TaskCompletionSource<string[]>?`)
- [ ] 1.2 Adicionar método `Cancel(sessionId)` em `WorkflowSessionManager` que chama `Cts.Cancel()` e retorna `bool` (false se sessão não encontrada)
- [ ] 1.3 Atualizar `Register()` em `WorkflowSessionManager` para aceitar e armazenar o novo `CancellationTokenSource`; ajustar `Respond()` para null-guard em `Tcs`
- [ ] 1.4 Atualizar `ExpireStale()` para remover também sessões cujo `Cts.IsCancellationRequested` é true e emitir `errorCode: "session_expired"` no payload SignalR

## 2. Endpoint de validação de entrada (backend)

- [ ] 2.1 Adicionar validação de `url` em `WorkflowStartEndpoint.Handle`: retornar `400` se vazia ou não for URL do YouTube (reusar regex de `TranscriptExecutor`)
- [ ] 2.2 Adicionar validação de `postType`: retornar `400` se não for `"storytelling"`, `"lista"` ou `"opiniao"`
- [ ] 2.3 Adicionar validação de `mode`: retornar `400` se não for `"automatico"` ou `"consultado"`
- [ ] 2.4 Registrar sessão com novo `CancellationTokenSource` em `WorkflowSessionManager` antes de iniciar `Task.Run`; passar `CancellationToken` para os executors
- [ ] 2.5 Adicionar catch de `OperationCanceledException` no `Task.Run` de `WorkflowStartEndpoint` que chama `Cleanup(sessionId)` sem emitir novo evento (executor já emitiu `cancelled`)

## 3. Endpoint de cancelamento (backend)

- [ ] 3.1 Criar `WorkflowCancelEndpoint.cs` com `DELETE /api/workflow/{sessionId}` que chama `sessionManager.Cancel(sessionId)` e retorna `200 OK` ou `404 Not Found`
- [ ] 3.2 Registrar o novo endpoint em `Program.cs`: `app.MapDelete("/api/workflow/{sessionId}", WorkflowCancelEndpoint.Handle)`

## 4. Propagação de CancellationToken nos executors (backend)

- [ ] 4.1 Adicionar `CancellationToken cancellationToken = default` em `TranscriptExecutor.ExecuteAsync`; repassar para `youtube.Videos.ClosedCaptions.GetManifestAsync` e `GetAsync`
- [ ] 4.2 Adicionar catch de `OperationCanceledException` em `TranscriptExecutor` que emite `{ step: "transcript", status: "error", errorCode: "cancelled", message: "Workflow cancelado." }`
- [ ] 4.3 Adicionar `CancellationToken cancellationToken = default` em `SummaryExecutor.ExecuteAsync`; repassar para `CompleteChatAsync`
- [ ] 4.4 Adicionar catch de `OperationCanceledException` em `SummaryExecutor` que emite `{ step: "summary", status: "error", errorCode: "cancelled", message: "Workflow cancelado." }`
- [ ] 4.5 Adicionar `CancellationToken cancellationToken = default` em `LinkedInWriterExecutor.ExecuteAsync` e métodos internos; repassar para `CompleteChatAsync` e `await tcs.Task` (via `WaitAsync(cancellationToken)`)
- [ ] 4.6 Adicionar catch de `OperationCanceledException` em `LinkedInWriterExecutor` que emite `{ step: "writing", status: "error", errorCode: "cancelled", message: "Workflow cancelado." }`

## 5. errorCode nos eventos SignalR existentes (backend)

- [ ] 5.1 Atualizar `TranscriptExecutor`: adicionar `errorCode: "video_inaccessible"` ao evento de `VideoUnavailableException`
- [ ] 5.2 Atualizar `TranscriptExecutor`: adicionar `errorCode: "no_transcript"` ao evento de sem legendas
- [ ] 5.3 Atualizar `TranscriptExecutor`: adicionar `errorCode: "llm_error"` ao catch genérico (ou manter nome descritivo)
- [ ] 5.4 Atualizar `SummaryExecutor`: adicionar `errorCode: "llm_error"` a todos os catches de erro LLM
- [ ] 5.5 Atualizar `LinkedInWriterExecutor`: adicionar `errorCode: "llm_error"` aos catches de erro LLM existentes

## 6. Tipos e serviços Angular (frontend)

- [ ] 6.1 Adicionar `errorCode?: string` ao tipo `WorkflowEvent` em `signalr.service.ts`
- [ ] 6.2 Adicionar método `cancel(sessionId: string): Observable<void>` em `WorkflowService` que envia `DELETE /api/workflow/${sessionId}`

## 7. Orquestração de erros no App (frontend)

- [ ] 7.1 Adicionar `lastError = signal<{ errorCode: string; message: string } | null>(null)` em `app.ts`
- [ ] 7.2 No handler `workflowEvent$`, detectar `status === 'error'` e popular `lastError` com `{ errorCode, message }` do evento; mudar `view` para `'form'` quando `errorCode === 'session_expired'`
- [ ] 7.3 Implementar `onCancel()` em `app.ts`: chama `workflowService.cancel(currentSessionId)`, restaura `view = 'form'` com todos os dados preservados, limpa `consultedQuestions` e `postDraft`
- [ ] 7.4 Implementar `onRetry()` diferenciado: se `lastError().errorCode === 'no_transcript'`, limpa `url`; caso contrário, preserva todos os campos; limpa `lastError`, `postDraft`, `consultedQuestions`
- [ ] 7.5 Atualizar `onReset()` para limpar `lastError` além dos estados já limpos

## 8. Template do App (frontend)

- [ ] 8.1 Envolver o formulário em `<fieldset [disabled]="view === 'progress'">` para bloquear todos os controles durante processamento
- [ ] 8.2 Adicionar botão "Cancelar" na view de progresso (`@else`) que chama `onCancel()`
- [ ] 8.3 Exibir mensagem de erro quando `lastError()` está populado (com `errorCode` e `message`)
- [ ] 8.4 Exibir botão contextual: "Tentar com outro vídeo" quando `errorCode === 'no_transcript'`; "Tentar Novamente" nos demais casos

## 9. CSS responsivo (frontend)

- [ ] 9.1 Adicionar media query `@media (max-width: 375px)` em `app.css`: inputs e botões com `width: 100%`, remover margens laterais
- [ ] 9.2 Adicionar media query `@media (max-width: 768px)` em `app.css`: container com `max-width: 100%` e padding lateral
- [ ] 9.3 Garantir que o container principal tem `max-width: 800px` e `margin: 0 auto` para desktop (1 280 px+)
