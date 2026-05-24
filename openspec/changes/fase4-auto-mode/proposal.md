## Why

O pipeline já produz um resumo estruturado do vídeo (Fase 3), mas ainda não gera o post para o LinkedIn. Esta fase fecha o loop do modo automático: dado o resumo e o tipo de post escolhido pelo usuário, a IA produz um rascunho completo, pronto para copiar e publicar.

## What Changes

- Adicionar `Prompts/linkedin-writer-system.md` com todas as regras de formatação, tom, SEO e estrutura da skill `criar-post-linkedin`
- Criar `Models/PostDraftResult.cs` (record com `Draft` e `TemplateUsed`)
- Criar `Executors/LinkedInWriterExecutor.cs` (Singleton) que recebe `summary` + `postType`, chama o Azure OpenAI e emite eventos SignalR
- Encadear `LinkedInWriterExecutor` após `SummaryExecutor` no `WorkflowStartEndpoint`
- Registrar `LinkedInWriterExecutor` em `Program.cs`
- Criar `PostDraftComponent` no frontend para exibir o rascunho gerado, o template utilizado, e os botões "Copiar" e "Gerar Novo Post"

## Capabilities

### New Capabilities

- `linkedin-writer-executor`: Executor backend que orquestra a chamada ao LLM com o prompt de escrita de posts, parseia a resposta JSON (`{ draft, templateUsed }`) e emite eventos SignalR `{ step: "writing", status: ... }`
- `post-draft-ui`: Componente Angular que exibe o resultado da geração — template utilizado, rascunho formatado com parágrafos corretos, botão "Copiar" com feedback visual e botão "Gerar Novo Post" para reset do estado

### Modified Capabilities

<!-- Nenhuma capability existente tem mudança de requisitos nesta fase -->

## Impact

- **Backend**: novos arquivos `LinkedInWriterExecutor.cs`, `PostDraftResult.cs`, `Prompts/linkedin-writer-system.md`; alteração em `WorkflowStartEndpoint.cs` (encadeamento) e `Program.cs` (registro DI)
- **Frontend**: novo componente `PostDraftComponent`; `AppComponent` ou componente pai passa a exibir o draft após o step `writing: completed`
- **SignalR**: novo step `"writing"` com status `in_progress`, `completed` e `error`
- **Dependências**: nenhuma nova — já usa `Azure.AI.OpenAI` SDK (adicionado na Fase 3)
