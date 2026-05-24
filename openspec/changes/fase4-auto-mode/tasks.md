## 1. Backend — Prompt e Modelo

- [ ] 1.1 Criar `Prompts/linkedin-writer-system.md` com as regras de formatação, SEO, tom, estrutura e saída JSON da skill `criar-post-linkedin`
- [ ] 1.2 Criar `Models/PostDraftResult.cs` com `public record PostDraftResult(string Draft, string TemplateUsed)`

## 2. Backend — LinkedInWriterExecutor

- [ ] 2.1 Criar `Executors/LinkedInWriterExecutor.cs` com injeção de `PromptLoader`, `IHubContext<WorkflowHub>`, `AzureOpenAIClient` e `IConfiguration`
- [ ] 2.2 Implementar `ExecuteAsync(string summary, string postType, string sessionId)`: emitir `in_progress`, montar user message com tipo de post e resumo, chamar Azure OpenAI (temperatura 0.7)
- [ ] 2.3 Implementar parse da resposta: sanitizar markdown fence (` ```json ` / ` ``` `), desserializar via `System.Text.Json` para `PostDraftResult`
- [ ] 2.4 Emitir evento SignalR `{ step: "writing", status: "completed", result: PostDraftResult }` em caso de sucesso
- [ ] 2.5 Implementar blocos `catch` para `RequestFailedException`, `TaskCanceledException` e `Exception`: emitir `{ step: "writing", status: "error", message: "Ocorreu um erro ao gerar o post. Tente novamente." }` e re-lançar
- [ ] 2.6 Registrar `LinkedInWriterExecutor` como `AddSingleton` em `Program.cs`
- [ ] 2.7 Atualizar `WorkflowStartEndpoint.Handle` para injetar `LinkedInWriterExecutor` e encadear `await linkedInWriterExecutor.ExecuteAsync(summary, request.PostType, sessionId)` após `SummaryExecutor`

## 3. Frontend — PostDraftComponent

- [ ] 3.1 Criar `PostDraftComponent` (standalone) com `@Input() draft: string` e `@Input() templateUsed: string`
- [ ] 3.2 Implementar exibição do `templateUsed` e do `draft` com espaçamento correto entre parágrafos (usar `white-space: pre-wrap` ou split por `\n\n`)
- [ ] 3.3 Implementar botão "Copiar": chamar `navigator.clipboard.writeText(draft)`, exibir "Copiado!" por 2 segundos via sinal temporário, depois retornar a "Copiar"
- [ ] 3.4 Implementar botão "Gerar Novo Post": emitir `EventEmitter` ou chamar serviço para resetar todos os sinais de estado do workflow
- [ ] 3.5 Conectar `PostDraftComponent` ao componente pai: receber `workflowEvent { step: "writing", status: "completed" }` via SignalR, popular `draft` e `templateUsed`, exibir o componente
- [ ] 3.6 Garantir que o reset via "Gerar Novo Post" oculta `PostDraftComponent` e `WorkflowProgressComponent` e exibe novamente o formulário de URL

## 4. Verificação

- [ ] 4.1 Testar fluxo completo modo Auto: URL → transcrição → resumo → rascunho exibido na UI com template e draft
- [ ] 4.2 Testar botão "Copiar": conteúdo copiado, feedback "Copiado!" aparece e desaparece após 2s
- [ ] 4.3 Testar botão "Gerar Novo Post": todos os estados resetados, formulário de URL exibido
- [ ] 4.4 Testar erro no LLM: evento `{ step: "writing", status: "error" }` exibido na UI sem quebrar os steps anteriores
