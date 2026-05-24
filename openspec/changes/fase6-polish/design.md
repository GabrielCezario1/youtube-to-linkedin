## Context

As fases 1–5 construíram o happy path completo — transcrição, sumarização, modo automático e modo consultado. O backend já possui tratamento de erros parcial (TranscriptExecutor captura `VideoUnavailableException`, SummaryExecutor captura exceções LLM, WorkflowSessionManager emite evento de expiração). Contudo:

- O endpoint `/start` não valida entrada — workflows são disparados com URLs vazias ou modos inválidos.
- Não há `CancellationToken` propagado pelos executors, então cancelamento pelo usuário não interrompe operações em curso.
- Não existe endpoint de cancelamento explícito.
- Os eventos SignalR de erro usam apenas `status: "error"` com mensagem de texto; o frontend não consegue diferenciar o tipo de erro para aplicar a lógica de retry correta (preservar URL vs. limpar URL).
- O formulário Angular não está bloqueado durante o processamento — o botão é o único guard.
- Não há responsividade CSS além do layout padrão.

**Stack atual**: ASP.NET Core 10 Minimal API · Azure.AI.OpenAI v2 · YoutubeExplode · SignalR · Angular 17+ standalone components.

## Goals / Non-Goals

**Goals:**
- Validação rápida de entrada em `/start` (falha antes de criar sessão/executors)
- `CancellationToken` propagado dos executors para todas as chamadas async (YoutubeExplode, OpenAI)
- Endpoint `DELETE /api/workflow/{sessionId}` para cancelamento pelo usuário
- Eventos SignalR de erro com campo `errorCode` semântico: `invalid_url`, `video_inaccessible`, `no_transcript`, `llm_error`, `session_expired`, `cancelled`
- Formulário Angular completamente bloqueado (fieldset `disabled`) durante processamento
- Botão "Cancelar" funcional em todas as etapas (incluindo pausa do modo Consultado)
- Lógica de retry diferenciada: `no_transcript` limpa URL; demais erros preservam URL
- CSS responsivo nos breakpoints 375 px, 768 px e 1 280 px
- Reset completo sem reload de página

**Non-Goals:**
- Testes automatizados (fora do escopo do PRD)
- Retry automático com back-off no backend
- Fila de jobs / persistência de sessões além do processo

## Decisions

### D1 — Validação no endpoint antes de `Task.Run`
Verificar URL, postType e mode em `WorkflowStartEndpoint.Handle` e retornar `400` com `{ error, field }` antes de criar o `sessionId`. Alternativa (validar nos executors) foi descartada porque cria sessão e conexão SignalR desnecessárias para entradas obviamente inválidas.

### D2 — `CancellationToken` por sessão via `CancellationTokenSource` no `WorkflowSessionManager`
`WorkflowSessionManager` passa a armazenar também um `CancellationTokenSource` por sessão. O `DELETE` endpoint chama `Cancel()` no CTS. O `WorkflowStartEndpoint` obtém o token e o passa para cada `ExecuteAsync`. Cada executor repassa o token para `YoutubeExplode`, `CompleteChatAsync`, etc. Alternativa (token global do host) foi descartada — colide com ciclo de vida da aplicação.

> `ActiveSession` já existe com `Tcs` e `PostType`; adicionamos `CancellationTokenSource Cts` à mesma classe. Para sessões de modo automático (sem pausa TCS), o `CancellationTokenSource` é registrado assim mesmo para suportar cancelamento.

### D3 — Escopo de registro: todas as sessões, não apenas consultado
Atualmente `WorkflowSessionManager` só armazena sessões que atingem a etapa de pausa do modo consultado. Para cancelamento, precisamos registrar a sessão desde o `POST /start`. O `ActiveSession.Tcs` torna-se nullable (`TaskCompletionSource<string[]>?`) para sessões de modo automático.

### D4 — `errorCode` como campo adicional no payload SignalR (não substituição de `message`)
Adicionar `errorCode` ao payload existente sem remover `message`. Frontend usa `errorCode` para lógica de retry; `message` continua sendo exibida para o usuário. Backward-compatible com qualquer listener futuro.

Tabela de mapeamento:

| Situação | `errorCode` | `step` | Retry UI |
|---|---|---|---|
| URL vazia / inválida | `invalid_url` | `transcript` | Preserva URL |
| Vídeo privado / removido | `video_inaccessible` | `transcript` | Preserva URL |
| Sem transcrição | `no_transcript` | `transcript` | Limpa URL |
| Erro LLM (summary) | `llm_error` | `summary` | Preserva URL |
| Erro LLM (writing) | `llm_error` | `writing` | Preserva URL |
| Timeout sessão consultada | `session_expired` | `writing` | Retorna ao form |
| Cancelamento explícito | `cancelled` | `*` | Preserva todos os dados |

### D5 — Formulário bloqueado via `<fieldset [disabled]>` Angular
Um `<fieldset>` com `[disabled]="isProcessing"` desabilita nativamente todos os controles filhos sem necessidade de lógica individual por campo. `isProcessing` é derivado de `view === 'progress'`.

### D6 — CSS responsivo sem framework adicional
Media queries nativas em `app.css` e `workflow-progress.css`. Não adicionar Tailwind ou Bootstrap — o projeto não os usa. Três breakpoints: `max-width: 375px` (mobile), `768px` (tablet), `1280px` (desktop).

## Risks / Trade-offs

- **[Risk] Race condition no cancelamento**: usuário cancela exatamente enquanto executor emite evento de conclusão → `OperationCanceledException` pode se perder na fila do `Task.Run`. Mitigação: catch explícito de `OperationCanceledException` nos executors que emite `cancelled` antes de relançar.

- **[Risk] Sessões modo automático nunca expiram no novo registro**: o sweep atual expira por `CreatedAt + 10min` e só existe para sessões consultadas. Com o novo registro universal, sessões automáticas também entrarão no sweep. Mitigação: tempo razoável (10 min) cobre qualquer workflow automático; o sweep também remove sessões cujo CTS já foi cancelado.

- **[Trade-off] `ActiveSession.Tcs` nullable**: introduz nullable no modelo que antes era sempre não-nulo. Mitigação: `Tcs` é propriedade `init`; o código de `Respond()` já verifica `TryGetValue` antes de usar — apenas adicionar null-guard.

## Migration Plan

1. Modificar `ActiveSession` (nullable `Tcs`, adicionar `Cts`)
2. Atualizar `WorkflowSessionManager` (registro universal, `Cancel`, sweep atualizado)
3. Atualizar `WorkflowStartEndpoint` (validação + registro de sessão + passar CT)
4. Adicionar `WorkflowCancelEndpoint` (`DELETE /api/workflow/{sessionId}`)
5. Atualizar executors (receber `CancellationToken`, emitir `errorCode`, repassar CT para libs)
6. Atualizar `Program.cs` (mapear novo endpoint)
7. Atualizar frontend (service, app.ts, app.html, CSS)

Rollback: todos os arquivos modificados estão sob controle de versão; não há schema de banco nem migração de dados.

## Open Questions

- Nenhuma questão em aberto — todos os cenários estão cobertos pelo PRD e pelas regras R1–R10.
