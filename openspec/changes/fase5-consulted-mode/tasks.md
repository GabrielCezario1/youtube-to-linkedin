## 1. Backend — WorkflowSessionManager

- [x] 1.1 Criar `ActiveSession.cs` em `Models/` com propriedades `SessionId`, `Tcs` (`TaskCompletionSource<string[]>`), `CreatedAt`, `PostType`
- [x] 1.2 Criar `WorkflowSessionManager.cs` em `Services/` com `ConcurrentDictionary<string, ActiveSession>`, métodos `Register`, `Respond` (retorna bool), `Cleanup`
- [x] 1.3 Adicionar `BackgroundService` (ou hosted service integrado ao manager) que percorre o dicionário a cada 60 s, cancela TCS de sessões com mais de 10 min via `TrySetCanceled()` e remove do dicionário
- [x] 1.4 Registrar `WorkflowSessionManager` como `Singleton` e como `IHostedService` em `Program.cs`

## 2. Backend — WorkflowRespondEndpoint

- [x] 2.1 Criar `WorkflowRespondEndpoint.cs` em `Endpoints/` com record `RespondWorkflowRequest(string[] Answers)` e handler `POST /api/workflow/{sessionId}/respond`
- [x] 2.2 Handler chama `WorkflowSessionManager.Respond(sessionId, answers)` e retorna `200 OK` se `true`, `404 Not Found` se `false`
- [x] 2.3 Mapear o novo endpoint em `Program.cs`

## 3. Backend — LinkedInWriterExecutor (modo Consultado)

- [x] 3.1 Adicionar parâmetro `mode` a `ExecuteAsync` e rotear para `ExecuteAutoAsync` ou `ExecuteConsultedAsync` conforme valor
- [x] 3.2 Implementar `GetFixedQuestions(postType)` — retorna lista de strings conforme o template (Storytelling / Lista Prática / Opinião Provocativa)
- [x] 3.3 Implementar `GetDynamicQuestionsAsync(summary)` — chama o LLM (temp 0.2, prompt pedindo JSON array com 1-3 perguntas) e retorna lista; em caso de falha retorna lista vazia
- [x] 3.4 Implementar `ExecuteConsultedAsync`: emite `in_progress`, gera perguntas (fixas + dinâmicas), emite `awaiting_input { questions }`, registra sessão no manager, faz `await tcs.Task`
- [x] 3.5 Após receber respostas, filtra strings em branco, monta prompt enriquecido, chama Azure OpenAI, emite `completed { result }` — igual ao modo Automático
- [x] 3.6 Capturar `OperationCanceledException` no `ExecuteConsultedAsync` e emitir `{ step: "writing", status: "error", message: "Sessão expirada. Inicie novamente." }`
- [x] 3.7 Atualizar `WorkflowStartEndpoint` para passar `request.Mode` como parâmetro `mode` ao chamar `linkedInWriterExecutor.ExecuteAsync`

## 4. Frontend — SignalR Service

- [x] 4.1 Adicionar `'awaiting_input'` ao tipo `StepStatus` em `signalr.service.ts`
- [x] 4.2 Adicionar campo `questions?: string[]` à interface `WorkflowEvent`
- [x] 4.3 Criar `WorkflowService` (ou adicionar método `respond` ao `SignalRService`) com `respond(sessionId: string, answers: string[]): Observable<void>` que chama `POST /api/workflow/{sessionId}/respond`

## 5. Frontend — ConsultedQuestionsComponent

- [x] 5.1 Gerar componente standalone `ConsultedQuestionsComponent` em `components/consulted-questions/`
- [x] 5.2 Adicionar `@Input() questions: string[] = []`, `@Input() sessionId = ''`, `@Output() submitted = new EventEmitter<void>()`
- [x] 5.3 Implementar template: lista de `<textarea>` (um por pergunta), nota "Todas as perguntas são opcionais", botão "Continuar"
- [x] 5.4 Implementar `onSubmit()`: coleta valores das textareas, chama `workflowService.respond(sessionId, answers)`, emite `submitted`

## 6. Frontend — WorkflowProgressComponent (estado awaiting_input)

- [x] 6.1 Atualizar o template de `WorkflowProgressComponent` para exibir ícone de pausa (⏸) e label "Aguardando sua resposta..." quando `status === 'awaiting_input'`

## 7. Frontend — App (orquestração do modo Consultado)

- [x] 7.1 Adicionar `consultedQuestions = signal<string[] | null>(null)` em `app.ts`
- [x] 7.2 No handler de `workflowEvent$`, detectar `step === 'writing' && status === 'awaiting_input'` e popular `consultedQuestions` com `event.questions`
- [x] 7.3 No handler de `workflowEvent$`, detectar `step === 'writing' && status === 'in_progress'` e limpar `consultedQuestions` (volta ao spinner)
- [x] 7.4 Em `app.html`, dentro do bloco de progresso, adicionar `@if (consultedQuestions()) { <app-consulted-questions [questions]="consultedQuestions()!" [sessionId]="currentSessionId!" (submitted)="onConsultedSubmitted()" /> }`
- [x] 7.5 Implementar `onConsultedSubmitted()` em `app.ts`: limpa `consultedQuestions` (componente é removido do DOM)
- [x] 7.6 Limpar `consultedQuestions` em `onReset()` e `onSubmit()`
