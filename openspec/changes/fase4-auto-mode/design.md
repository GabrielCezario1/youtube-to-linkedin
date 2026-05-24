## Context

O backend é uma ASP.NET Core 10 minimal-API. `TranscriptExecutor` e `SummaryExecutor` já estão implementados como Singletons e encadeados em `WorkflowStartEndpoint`. O `PromptLoader` singleton carrega arquivos `Prompts/*.md` na inicialização. `AzureOpenAIClient` está registrado como Singleton compartilhado.

Esta fase adiciona `LinkedInWriterExecutor`, que recebe o resumo e o tipo de post, chama o Azure OpenAI com um prompt específico para escrita de posts no LinkedIn, e emite eventos SignalR `{ step: "writing", ... }`. No frontend, um novo `PostDraftComponent` exibe o rascunho gerado.

## Goals / Non-Goals

**Goals:**
- Criar `LinkedInWriterExecutor` (Singleton) que segue exatamente o mesmo padrão de `SummaryExecutor`.
- Criar `PostDraftResult` record com `Draft` e `TemplateUsed`.
- Criar `Prompts/linkedin-writer-system.md` com as regras da skill `criar-post-linkedin`.
- Encadear `LinkedInWriterExecutor` após `SummaryExecutor` em `WorkflowStartEndpoint`, usando `PostType` já presente em `StartWorkflowRequest`.
- Criar `PostDraftComponent` no Angular com exibição do template utilizado, rascunho formatado, botão "Copiar" e botão "Gerar Novo Post".

**Non-Goals:**
- Modo Consultado (Fase 5) — `LinkedInWriterExecutor` não emite `awaiting_input`.
- Edição inline do rascunho na UI.
- Histórico de posts gerados.
- Regeneração com parâmetros diferentes sem reload completo.
- Streaming da resposta do LLM.
- WorkflowFactory ou qualquer abstração de orquestração.

## Decisions

### D1 — `LinkedInWriterExecutor` segue o mesmo padrão de `SummaryExecutor`
`SummaryExecutor` já demonstrou o padrão correto: injeção via construtor, `PromptLoader` para o system prompt, `AzureOpenAIClient` compartilhado, eventos SignalR via `IHubContext<WorkflowHub>`. Replicar o padrão mantém a codebase coerente e previsível.

*Alternativa considerada*: Classe base ou interface `IExecutor<TIn, TOut>` — rejeitada por ser prematura (apenas 3 executors no MVP); introduziria abstração sem benefício tangível neste momento.

### D2 — LLM retorna JSON estruturado `{ draft, templateUsed }`
O PRD especifica que a IA retorna JSON. Isso permite ao frontend exibir o template utilizado separadamente do rascunho, dando transparência ao usuário.

Parse via `System.Text.Json.JsonSerializer.Deserialize<PostDraftResult>` após limpar eventual markdown fence (` ```json ... ``` `). Em caso de falha no parse, emitir evento `error`.

*Alternativa considerada*: Parsear o template do texto livre — rejeitada pela fragilidade do parsing heurístico.

### D3 — `postType` passado no user message, system prompt reutilizável
O system prompt (`linkedin-writer-system.md`) contém as regras gerais. O tipo de post é instruído no user message (`"Tipo de post: lista"` + resumo). Isso permite reusar o mesmo system prompt para os 3 templates sem variações de arquivo.

### D4 — `PostDraftResult` como C# record
Consistente com o padrão já estabelecido (`StartWorkflowRequest` é um record). Imutabilidade e desestruturação sem boilerplate.

### D5 — Botão "Gerar Novo Post" reseta o estado completo do workflow
Ao clicar em "Gerar Novo Post", o frontend reseta todos os sinais de estado (transcript, summary, draft, progress steps) e volta ao estado inicial (formulário de URL). Isso é implementado no serviço de estado já existente sem criar um endpoint de reset no backend.

### D6 — Temperatura 0.7 para o LLM de escrita criativa
`SummaryExecutor` usa temperatura 0.3 (saída determinística para extração de fatos). `LinkedInWriterExecutor` usa 0.7 para maior variação criativa no rascunho, conforme o caráter criativo da tarefa.

## Risks / Trade-offs

| Risk | Mitigation |
|---|---|
| LLM pode retornar texto fora do JSON esperado (markdown fence, texto extra) | Sanitizar a resposta: remover ` ```json ` / ` ``` ` antes do parse; log do texto bruto em caso de erro |
| Post gerado pode não seguir as regras de tamanho (200–320 palavras) | Regra no system prompt; sem validação programática no MVP (verificação visual pelo usuário) |
| `postType` inválido enviado pelo frontend | `StartWorkflowRequest` já recebe `PostType` como string; o executor passa o valor diretamente ao LLM — string inesperada resulta em output degradado mas não em erro de sistema |
| `linkedInWriterExecutor` falha após `SummaryExecutor` já ter concluído | Erro emitido via SignalR `{ step: "writing", status: "error" }`; usuário vê o step com falha; resume e transcript ainda exibidos na UI |

## Migration Plan

1. Criar `Prompts/linkedin-writer-system.md`.
2. Criar `Models/PostDraftResult.cs`.
3. Criar `Executors/LinkedInWriterExecutor.cs`.
4. Registrar `LinkedInWriterExecutor` em `Program.cs` como Singleton.
5. Atualizar `WorkflowStartEndpoint` para injetar e encadear `LinkedInWriterExecutor` após `SummaryExecutor`.
6. Criar `PostDraftComponent` no Angular.
7. Conectar `PostDraftComponent` ao fluxo SignalR existente no componente pai.

**Rollback**: Remover `LinkedInWriterExecutor` da injeção no endpoint e comentar o `await` encadeado — os steps anteriores continuam funcionando.

## Open Questions

Nenhuma questão aberta — PRD define todos os detalhes necessários para implementação.
