# Plano de Implementação — Tech Content Agent

> **Versão:** 1.1
> **Data:** 2026-05-24
> **Atualizado:** 2026-05-24 — Correções pós-exploração da API real do Agent Framework
> **Referência:** [PRD_TechContentAgent.md](./PRD_TechContentAgent.md)

---

## Visão Geral da Arquitetura

```
┌──────────────────────────────────────────────────────────────┐
│                     Angular SPA                              │
│   WorkflowFormComponent                                      │
│   WorkflowProgressComponent                                  │
│   ConsultedQuestionsComponent                                │
│   PostDraftComponent                                         │
│   ErrorComponent                                             │
└───────────────────────┬──────────────────────────────────────┘
                        │  HTTP (start/respond) + SignalR
┌───────────────────────▼──────────────────────────────────────┐
│                  .NET 10 Minimal API                         │
│                                                              │
│   POST   /api/workflow/start                                 │
│   POST   /api/workflow/{sessionId}/respond                   │
│   Hub    /hubs/workflow  (SignalR)                           │
│                                                              │
├──────────────────────────────────────────────────────────────┤
│               Microsoft Agent Framework                      │
│                                                              │
│   WorkflowFactory (WorkflowBuilder)                          │
│     ├── TranscriptExecutor   (YoutubeExplode 6.6.0)          │
│     ├── SummaryExecutor      (LLM: pontos-chave)             │
│     └── LinkedInWriterExecutor                               │
│           ├── Auto mode:      gera diretamente               │
│           └── Consulted mode: RequestPort → SendResponseAsync │
│                                                              │
├──────────────────────────────────────────────────────────────┤
│              Infraestrutura / Integrações                    │
│                                                              │
│   YoutubeExplode 6.6.0  (NuGet)                              │
│   Azure OpenAI / OpenAI (LLM provider)                       │
│   In-memory state       (workflow sessions)                  │
└──────────────────────────────────────────────────────────────┘
```

---

## Estrutura de Pastas da Solution

```
youtube-to-linkedin/
├── docs/
│   ├── PRD_TechContentAgent.md
│   └── IMPLEMENTATION_PLAN.md
│
├── src/
│   ├── backend/
│   │   └── YoutubeToLinkedIn.Api/          ← .NET 10 Minimal API
│   │       ├── Executors/
│   │       │   ├── TranscriptExecutor.cs
│   │       │   ├── SummaryExecutor.cs
│   │       │   └── LinkedInWriterExecutor.cs
│   │       ├── WorkflowFactory.cs          ← WorkflowBuilder
│   │       └── WorkflowSessionManager.cs   ← armazena StreamingRun handles
│   │       ├── Hubs/
│   │       │   └── WorkflowHub.cs          ← SignalR Hub
│   │       ├── Endpoints/
│   │       │   ├── WorkflowStartEndpoint.cs
│   │       │   └── WorkflowRespondEndpoint.cs
│   │       ├── Models/
│   │       │   ├── StartWorkflowRequest.cs
│   │       │   ├── RespondWorkflowRequest.cs
│   │       │   ├── WorkflowProgressEvent.cs
│   │       │   └── PostDraftResult.cs
│   │       ├── Prompts/
│   │       │   ├── summarizer-system.md    ← prompt do SummaryNode
│   │       │   └── linkedin-writer-system.md ← prompt com regras da skill
│   │       ├── Program.cs
│   │       └── appsettings.json
│   │
│   └── frontend/
│       └── youtube-to-linkedin-app/       ← Angular 17+
│           ├── src/
│           │   ├── app/
│           │   │   ├── components/
│           │   │   │   ├── workflow-form/
│           │   │   │   ├── workflow-progress/
│           │   │   │   ├── consulted-questions/
│           │   │   │   ├── post-draft/
│           │   │   │   └── error-display/
│           │   │   ├── services/
│           │   │   │   ├── workflow.service.ts    ← HTTP calls
│           │   │   │   └── signalr.service.ts     ← SignalR client
│           │   │   ├── models/
│           │   │   │   └── workflow.models.ts
│           │   │   └── app.component.ts
│           │   └── environments/
│           └── package.json
│
└── README.md
```

---

## Fases de Implementação

---

### Fase 1 — Scaffold e Setup do Projeto

**Objetivo:** Ter a solução criada, compilando, com SignalR funcionando end-to-end.

**Backend:**
- [ ] Criar solution `.sln` e projeto `YoutubeToLinkedIn.Api`
- [ ] Instalar pacotes NuGet:
  - `Microsoft.Agents.AI` (Agent Framework)
  - `YoutubeExplode` (6.6.0)
  - `Microsoft.AspNetCore.SignalR`
  - `Azure.AI.OpenAI` ou `OpenAI`
- [ ] Configurar `Program.cs`: CORS, SignalR, Minimal API
- [ ] Criar `WorkflowHub.cs` com método de broadcast básico
- [ ] Criar endpoint `POST /api/workflow/start` (retorna sessionId mockado)
- [ ] Testar: POST → recebe sessionId → SignalR envia evento de progresso

**Frontend:**
- [ ] Criar projeto Angular com Angular CLI
- [ ] Instalar `@microsoft/signalr`
- [ ] Criar `SignalRService` com conexão ao hub
- [ ] Criar `WorkflowService` com método `start()`
- [ ] Criar `AppComponent` com formulário simples
- [ ] Testar: submeter formulário → ver evento SignalR no console

**Critério de conclusão:** POST ao backend retorna sessionId e o frontend recebe ao menos 1 evento SignalR em tempo real.

---

### Fase 2 — Extração de Transcrição (TranscriptNode)

**Objetivo:** Dado uma URL do YouTube, extrair e retornar a transcrição em texto.

**Backend:**
- [ ] Criar `TranscriptNode.cs` usando `YoutubeExplode`
- [ ] Extrair `videoId` da URL (suportar formatos: `?v=`, `youtu.be/`, `shorts/`)
- [ ] Tratar erros: vídeo privado, sem transcrição, URL inválida
- [ ] Emitir evento SignalR `{ step: "transcript", status: "in_progress" }` ao iniciar
- [ ] Emitir evento SignalR `{ step: "transcript", status: "completed" }` ao concluir
- [ ] Emitir evento SignalR `{ step: "transcript", status: "error", message: "..." }` em falha

**Frontend:**
- [ ] Criar `WorkflowProgressComponent` que escuta eventos SignalR
- [ ] Renderizar 3 etapas com estado: pendente / em andamento / concluída / erro
- [ ] Criar `ErrorDisplayComponent` com mensagem descritiva + botão "Tentar Novamente"

**Critério de conclusão:** URL válida → transcrição extraída → progresso atualizado na UI. URL inválida ou privada → erro descritivo exibido com opção de retry.

---

### Fase 3 — Resumo do Conteúdo (SummaryNode)

**Objetivo:** Dado a transcrição bruta, gerar um resumo estruturado com os pontos-chave do vídeo.

**Backend:**
- [ ] Configurar cliente LLM (Azure OpenAI ou OpenAI) em `Program.cs`
- [ ] Criar prompt `summarizer-system.md`:
  - Instrução: extrair 5–8 pontos-chave relevantes para um post técnico no LinkedIn
  - Formato de saída: lista numerada com título curto + 1 linha de contexto
- [ ] Criar `SummaryNode.cs` que chama o LLM com a transcrição
- [ ] Emitir eventos SignalR para a etapa "summary" (in_progress / completed / error)

**Frontend:**
- [ ] Atualizar `WorkflowProgressComponent` para refletir a etapa de resumo

**Critério de conclusão:** Transcrição → LLM → resumo estruturado retornado e etapa marcada como concluída na UI.

---

### Fase 4 — Geração do Post (LinkedInWriterNode — Modo Auto)

**Objetivo:** Dado o resumo e o tipo de post, gerar o rascunho completo no formato correto.

**Backend:**
- [ ] Criar prompt `linkedin-writer-system.md` incorporando as regras da skill `criar-post-linkedin`:
  - Os 3 templates (Storytelling, Lista Prática, Opinião Provocativa)
  - Regras de formatação: parágrafos curtos, emojis 0–2, hashtags 3–5
  - Regras de SEO: keyword no hook, nomes completos, 200–320 palavras
  - Tom: pessoal, direto, primeira pessoa
- [ ] Criar `LinkedInWriterNode.cs` — modo Auto:
  - Recebe: resumo + tipo de post
  - Escolhe estrutura do template automaticamente
  - Gera rascunho completo
  - Informa qual template foi utilizado no resultado
- [ ] Criar modelo `PostDraftResult.cs`:
  ```
  { draft: string, templateUsed: string }
  ```
- [ ] Emitir evento SignalR `{ step: "writing", status: "completed", result: PostDraftResult }`

**Frontend:**
- [ ] Criar `PostDraftComponent`:
  - Exibir template utilizado
  - Exibir rascunho com formatação respeitada
  - Botão "Copiar" com feedback visual ("Copiado!")
  - Botão "Gerar Novo Post" que reseta o estado da aplicação

**Critério de conclusão:** Fluxo completo funcional no modo Auto — URL → transcrição → resumo → post gerado → exibido na UI com opção de copiar.

---

### Fase 5 — Human-in-the-Loop (LinkedInWriterNode — Modo Consultado)

**Objetivo:** Antes de gerar o post, pausar o workflow, emitir perguntas e aguardar resposta do usuário.

**Backend:**
- [ ] Estender `LinkedInWriterNode.cs` para o modo Consultado:
  - Após receber o resumo, gerar perguntas (base fixa por template + dinâmicas por conteúdo)
  - Emitir evento SignalR `{ step: "writing", status: "awaiting_input", questions: [...] }`
  - Pausar o workflow (checkpoint in-memory com `sessionId`)
  - Aguardar chamada `POST /api/workflow/{sessionId}/respond`
- [ ] Criar `WorkflowSessionManager.cs`:
  - Armazena sessões ativas em `ConcurrentDictionary`
  - Guarda: `sessionId`, `TaskCompletionSource` (para retomar o workflow), estado atual
- [ ] Criar endpoint `POST /api/workflow/{sessionId}/respond`:
  - Recebe respostas do usuário (`{ answers: string[] }`)
  - Resolve o `TaskCompletionSource` → workflow retoma
- [ ] Perguntas fixas por template (base):
  - **Storytelling:** "Qual foi o erro ou obstáculo principal?", "Qual foi o aprendizado mais valioso?", "Para quem é este post?"
  - **Lista Prática:** "Algum item da lista tem contexto da sua experiência pessoal?", "Para quem é este post?"
  - **Opinião Provocativa:** "Qual é a crença comum que você quer questionar?", "Você tem um dado ou exemplo concreto para reforçar?"

**Frontend:**
- [ ] Criar `ConsultedQuestionsComponent`:
  - Renderiza lista de perguntas dinamicamente
  - Campo de texto livre por pergunta
  - Indicação visual de que todas são opcionais
  - Botão "Continuar" que chama `POST /api/workflow/{sessionId}/respond`
- [ ] Atualizar `SignalRService` para detectar evento `awaiting_input` e exibir o componente

**Critério de conclusão:** Fluxo completo no modo Consultado — workflow pausa, UI exibe perguntas, usuário responde (ou pula) e post é gerado com o contexto fornecido.

---

### Fase 6 — Polimento e Tratamento de Erros

**Objetivo:** Garantir que todos os cenários de erro do PRD estão cobertos e a UX está consistente.

**Backend:**
- [ ] Timeout de sessão: cancelar workflow se usuário não responder em X minutos
- [ ] Validação de URL no endpoint de start (antes de iniciar o workflow)
- [ ] Mensagens de erro específicas para cada tipo de falha (privado, sem transcrição, LLM)

**Frontend:**
- [ ] Bloquear formulário durante processamento
- [ ] Desabilitar botão "Gerar Post" durante processamento
- [ ] Botão "Cancelar" funcional durante qualquer etapa
- [ ] Retry preserva dados do formulário
- [ ] Reset limpa todos os estados ao clicar "Gerar Novo Post"
- [ ] Responsividade básica da página

**Critério de conclusão:** Todos os cenários de erro do PRD funcionando. UX consistente em happy path e error paths.

---

### Fase 7 — Configuração e README

**Objetivo:** Projeto configurável e documentado para execução local.

- [ ] `appsettings.json` + `appsettings.Development.json` com chaves de configuração:
  - `OpenAI:ApiKey` ou `AzureOpenAI:Endpoint` + `ApiKey`
  - `OpenAI:ModelId`
- [ ] `.env.example` no frontend com URL do backend
- [ ] `README.md` com:
  - Pré-requisitos (Node, .NET 9, chave OpenAI)
  - Passos para rodar localmente (backend + frontend)
  - Link para o PRD
- [ ] `.gitignore` adequado (segredos, node_modules, build artifacts)

---

## Dependências entre Fases

```
Fase 1 (Scaffold)
    │
    ├──▶ Fase 2 (Transcript)
    │         │
    │         └──▶ Fase 3 (Summary)
    │                   │
    │                   ├──▶ Fase 4 (Auto Mode) ──▶ Fase 6 (Polish)
    │                   │                                   │
    │                   └──▶ Fase 5 (Consulted Mode) ───────┘
    │                                                       │
    └──────────────────────────────────────────────────────▶ Fase 7 (Docs)
```

---

## Pacotes e Versões

### Backend (NuGet)

| Pacote | Uso |
|---|---|
| `Microsoft.Agents.AI` (prerelease) | Agent Framework — agentes e modelo |
| `Microsoft.Agents.AI.Workflows` (prerelease) | Agent Framework — workflow graph e executors |
| `YoutubeExplode` (6.6.0) | Extração de transcrição e legendas do YouTube |
| `Azure.AI.OpenAI` | Cliente LLM (Azure OpenAI) |
| `Microsoft.AspNetCore.SignalR` | Streaming em tempo real + human-in-the-loop |

### Frontend (npm)

| Pacote | Uso |
|---|---|
| `@microsoft/signalr` | Cliente SignalR para Angular |
| `@angular/core` 17+ | Framework frontend |

---

## Decisões de Implementação

| Ponto | Decisão |
|---|---|
| State management do workflow | `ConcurrentDictionary<string, StreamingRun>` in-memory — armazena handles do Agent Framework |
| Human-in-the-loop | `RequestPort` nativo do framework + `SendResponseAsync()` — sem `TaskCompletionSource` manual |
| Terminologia dos executores | `Executor` (alinhado com Agent Framework) — não `Node` como estava no plano original |
| Criação do workflow | `WorkflowFactory.cs` com `WorkflowBuilder` — não `WorkflowDefinition.cs` |
| Runtime | .NET 10 (já instalado na máquina) |
| LLM provider | Azure OpenAI (decisão tomada na exploração) |
| Angular | 21.2.8, standalone components |
| Formato do `sessionId` | GUID gerado no backend no momento do `start` |
| Timeout de sessão consultada | 10 minutos sem resposta → cancelar e emitir evento de timeout |
| Extração de `videoId` | Regex cobrindo `?v=`, `youtu.be/`, `/shorts/` |
| Prompts | Arquivos `.md` em `Prompts/` carregados em startup — facilita edição sem recompilar |
| CORS | Liberado para `localhost:4200` em desenvolvimento |
| Porta backend | `https://localhost:5001` (padrão .NET) |
| Porta frontend | `http://localhost:4200` (padrão Angular CLI) |
