## Context

O projeto usa ASP.NET Core 10 Minimal API com o executor pattern: `TranscriptExecutor → SummaryExecutor → LinkedInWriterExecutor`, orquestrados em `Task.Run` fire-and-forget dentro de `WorkflowStartEndpoint`. O `LinkedInWriterExecutor` da Fase 4 executa de forma síncrona e sem interrupção.

O modo Consultado exige que o executor **pause** após gerar as perguntas e **retome** quando o usuário responder — um padrão human-in-the-loop clássico. O PRD especifica o uso do `RequestPort` do Agent Framework para esta pausa, mantendo o fluxo dentro do mesmo `Task.Run` sem necessidade de serialização de estado.

A comunicação em tempo real já está implementada via SignalR (`IHubContext<WorkflowHub>`). O frontend já consome `workflowEvent$`. O único novo status necessário é `awaiting_input`.

## Goals / Non-Goals

**Goals:**
- Pausar `LinkedInWriterExecutor` após emitir perguntas via `RequestPort`
- Gerenciar sessões pausadas com timeout de 10 minutos via `WorkflowSessionManager`
- Expor `POST /api/workflow/{sessionId}/respond` para retomar o workflow
- Exibir `ConsultedQuestionsComponent` no frontend quando `status: "awaiting_input"` é recebido
- Gerar perguntas dinâmicas adicionais via chamada LLM extra (máx 3)
- Preservar o modo Automático completamente inalterado

**Non-Goals:**
- Persistência das sessões entre restarts do servidor
- Edição das perguntas pelo usuário
- Mais de 3 perguntas dinâmicas
- Histórico de interações por sessão

## Decisions

### D1 — Pausa via `TaskCompletionSource<string[]>` (não Agent Framework RequestPort)

O PRD menciona `RequestPort` do Agent Framework, mas o projeto **não tem** o Agent Framework como dependência — usa Azure.AI.OpenAI diretamente. Introduzir o SDK completo do Agent Framework para um único padrão de pausa seria uma dependência pesada e desnecessária.

**Decisão**: usar `TaskCompletionSource<string[]>` gerenciado pelo `WorkflowSessionManager`. O executor armazena o `TCS` e chama `await tcs.Task` para pausar. O endpoint `/respond` chama `TCS.SetResult(answers)` para retomar. Semanticamente idêntico ao `RequestPort`, sem dependência externa.

**Alternativas consideradas**:
- `RequestPort` do Agent Framework: exigiria adicionar o SDK como dependência; over-engineering para um único caso de uso
- Canal de mensagens (`Channel<T>`): mais complexo, sem benefício aqui

### D2 — `WorkflowSessionManager` como Singleton in-memory com `ConcurrentDictionary`

Sessões são registradas com `TCS + metadata` e expiradas por um `BackgroundService` a cada minuto. Sem banco de dados — MVP sem necessidade de persistência entre restarts.

**Registros**: `ConcurrentDictionary<string, ActiveSession>` onde `ActiveSession` contém `TaskCompletionSource<string[]>`, `DateTime CreatedAt`, `string PostType`.

### D3 — Perguntas fixas por template + até 3 dinâmicas via LLM

As perguntas fixas (definidas no PRD por template) são retornadas sem custo. As dinâmicas exigem uma chamada LLM adicional com temperatura baixa (0.2) pedindo ao modelo 1-3 perguntas específicas do conteúdo em formato JSON. Se a chamada LLM falhar, o executor continua apenas com as fixas — sem bloquear o fluxo.

### D4 — Prompt do modo Consultado com contexto de respostas

O prompt enviado ao LLM para geração do post no modo Consultado é o mesmo `linkedin-writer-system.md` da Fase 4. A mensagem do usuário é enriquecida com as respostas: `Tipo de post: {postType}\n\nResumo:\n{summary}\n\nContexto adicional do autor:\n{answers}`. Respostas em branco são filtradas antes de montar o contexto.

### D5 — Frontend: evento `awaiting_input` tratado em `app.ts`

Assim como `writing.completed` já é detectado em `app.ts` para exibir `PostDraftComponent`, o novo `writing.awaiting_input` também será detectado em `app.ts` para popular `consultedQuestions` e exibir o `ConsultedQuestionsComponent`. O componente emite um `(submit)` com as respostas, que `app.ts` passa para `WorkflowService.respond()`.

### D6 — `StepStatus` estendido com `'awaiting_input'`

No `signalr.service.ts`, `StepStatus` ganha `'awaiting_input'`. O `WorkflowProgressComponent` exibirá `⏸` para este status. Não é um estado de erro, portanto não aciona o `ErrorDisplayComponent`.

## Risks / Trade-offs

| Risco | Mitigação |
|---|---|
| Leak de memória se muitas sessões ficam abertas | Background task expira sessões após 10 min; limite de sessões não necessário para MVP |
| LLM falha ao gerar perguntas dinâmicas | Catch silencioso — fluxo continua apenas com perguntas fixas |
| Usuário fecha o browser sem responder | Timeout de 10 min cancela a `TCS` via `CancellationToken` e emite evento de erro via SignalR |
| `TCS.SetResult` chamado após timeout | Verificação de `Task.IsCompleted` antes de chamar `SetResult` no endpoint `/respond` |
| Race condition: respond chega antes de register | Não possível — `Register` é chamado antes de `SendWorkflowEvent`, e `SendWorkflowEvent` é anterior ao cliente poder chamar `/respond` |
