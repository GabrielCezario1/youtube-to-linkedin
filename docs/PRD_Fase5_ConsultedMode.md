# PRD — Fase 5: Human-in-the-Loop — Modo Consultado (LinkedInWriterExecutor)

> **Versão:** 1.0
> **Data:** 2026-05-24
> **Depende de:** [PRD_Fase4_AutoMode.md](./PRD_Fase4_AutoMode.md)
> **Referência:** [IMPLEMENTATION_PLAN.md](./IMPLEMENTATION_PLAN.md)

---

## Objetivo

No modo Consultado, o workflow pausa após o resumo, emite perguntas contextuais ao usuário via SignalR, aguarda as respostas e retoma a geração do post enriquecido com o contexto fornecido.

---

## Fluxo Completo do Modo Consultado

```
  SummaryExecutor
       │
       │  summary + postType + mode: "consultado"
       ▼
┌──────────────────────────────────────────────────────────┐
│         LinkedInWriterExecutor (Consultado)              │
│                                                          │
│  1. Gera perguntas (base fixa + dinâmicas do LLM)        │
│  2. Emite SignalR awaiting_input { questions: [...] }    │
│  3. PAUSA via RequestPort (Agent Framework)              │
└──────────────────────────┬───────────────────────────────┘
                           │  (workflow pausado)
                           │
    ┌──────────────────────▼─────────────────────┐
    │              Frontend                      │
    │  ConsultedQuestionsComponent exibido       │
    │  Usuário responde (ou não) e clica         │
    │  "Continuar"                               │
    └──────────────────────┬─────────────────────┘
                           │
                           │  POST /api/workflow/{sessionId}/respond
                           │  { answers: string[] }
                           ▼
┌──────────────────────────────────────────────────────────┐
│         WorkflowSessionManager                           │
│  Resolve RequestPort → workflow retoma                   │
└──────────────────────────┬───────────────────────────────┘
                           │
                           ▼
┌──────────────────────────────────────────────────────────┐
│         LinkedInWriterExecutor (continuação)             │
│                                                          │
│  4. Recebe respostas                                     │
│  5. Monta prompt com resumo + respostas do usuário       │
│  6. Chama Azure OpenAI                                   │
│  7. Emite SignalR completed { result: PostDraftResult }  │
└──────────────────────────────────────────────────────────┘
```

---

## Perguntas por Template

```
┌─────────────────────────────────────────────────────────────────┐
│                  PERGUNTAS FIXAS POR TEMPLATE                   │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  STORYTELLING                                                   │
│  1. "Qual foi o erro ou obstáculo principal?"                   │
│  2. "Qual foi o aprendizado mais valioso?"                      │
│  3. "Para quem é este post?"                                    │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  LISTA PRÁTICA                                                  │
│  1. "Algum item da lista tem contexto da sua experiência?"      │
│  2. "Para quem é este post?"                                    │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  OPINIÃO PROVOCATIVA                                            │
│  1. "Qual é a crença comum que você quer questionar?"           │
│  2. "Você tem um dado ou exemplo concreto para reforçar?"       │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  + até 3 perguntas DINÂMICAS geradas pelo LLM                  │
│    com base no conteúdo específico do vídeo                     │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## Timeout da Sessão Consultada

```
  Workflow pausado esperando resposta
              │
              │
      ┌───────▼───────┐
      │  10 minutos   │  sem resposta do usuário
      └───────┬───────┘
              │
              ▼
  WorkflowSessionManager cancela a sessão
              │
              ▼
  SignalR emite:
  { step: "writing", status: "error",
    message: "Sessão expirada. Inicie novamente." }
```

---

## Endpoints

### `POST /api/workflow/{sessionId}/respond`

```
Request:
{
  "answers": [
    "resposta da pergunta 1",
    "",                         ← em branco = não respondida
    "resposta da pergunta 3"
  ]
}

Response: 200 OK (sem body)
          404 Not Found  (sessionId não existe)
          408 Timeout    (sessão já expirou)
```

---

## WorkflowSessionManager

```
┌──────────────────────────────────────────────────────┐
│             WorkflowSessionManager                   │
├──────────────────────────────────────────────────────┤
│                                                      │
│  Storage:                                            │
│  ConcurrentDictionary<string, ActiveSession>         │
│                                                      │
│  ActiveSession:                                      │
│    SessionId:    string                              │
│    RequestPort:  IRequestPort  (Agent Framework)     │
│    CreatedAt:    DateTime      (para timeout)        │
│    PostType:     string                              │
│                                                      │
│  Métodos:                                            │
│    Register(sessionId, requestPort)                  │
│    Respond(sessionId, answers[]) → bool              │
│    Cleanup(sessionId)                                │
│    ExpireStale(threshold: 10min)  ← background task │
│                                                      │
└──────────────────────────────────────────────────────┘
```

---

## Eventos SignalR desta Fase

| Evento | Payload | Quando |
|---|---|---|
| `writing` | `{ step: "writing", status: "awaiting_input", questions: string[] }` | Workflow pausado, perguntas emitidas |
| `writing` | `{ step: "writing", status: "in_progress" }` | Após receber respostas, retomando |
| `writing` | `{ step: "writing", status: "completed", result: PostDraftResult }` | Post gerado |
| `writing` | `{ step: "writing", status: "error", message: "Sessão expirada..." }` | Timeout de 10 min |

---

## Regras e Decisões

| # | Regra / Decisão | Justificativa |
|---|---|---|
| R1 | Human-in-the-loop via **`RequestPort` nativo do Agent Framework** | Framework já provê o mecanismo; sem `TaskCompletionSource` manual |
| R2 | Timeout de **10 minutos** sem resposta → cancelar sessão | Evita leak de memória com sessões abertas indefinidamente |
| R3 | Todas as perguntas são **opcionais** | PRD principal; o usuário pode clicar "Continuar" sem responder |
| R4 | Máximo de **3 perguntas dinâmicas** além das fixas | Evitar sobrecarga do usuário |
| R5 | Respostas em branco são **aceitas e ignoradas** no prompt final | Usuário pode pular qualquer pergunta |
| R6 | Sessões armazenadas em **`ConcurrentDictionary` in-memory** | MVP sem banco; sem necessidade de persistência entre restarts |
| R7 | Endpoint `/respond` retorna **404** se `sessionId` não existe | Sessão expirada ou ID inválido |
| R8 | `ConsultedQuestionsComponent` é **oculto** após "Continuar" | UX; evitar re-submissão acidental |
| R9 | Modo é determinado no **`WorkflowFactory`** ao montar o grafo | `LinkedInWriterExecutor` recebe o modo como parâmetro |
| R10 | Perguntas dinâmicas são geradas com **chamada LLM adicional** antes da pausa | Personaliza a experiência por conteúdo do vídeo |

---

## UI — ConsultedQuestionsComponent

```
┌───────────────────────────────────────────────────────────┐
│              ConsultedQuestionsComponent                  │
├───────────────────────────────────────────────────────────┤
│                                                           │
│  ✅  Extraindo transcrição do vídeo                       │
│  ✅  Resumindo conteúdo                                   │
│  ⏸   Gerando rascunho do post  ← pausado                 │
│                                                           │
├───────────────────────────────────────────────────────────┤
│                                                           │
│  💬  Algumas perguntas para personalizar seu post         │
│      (todas opcionais)                                    │
│                                                           │
│  Qual foi o erro ou obstáculo principal?                  │
│  ┌──────────────────────────────────────────────────┐     │
│  │                                                  │     │
│  └──────────────────────────────────────────────────┘     │
│                                                           │
│  Qual foi o aprendizado mais valioso?                     │
│  ┌──────────────────────────────────────────────────┐     │
│  │                                                  │     │
│  └──────────────────────────────────────────────────┘     │
│                                                           │
│  Para quem é este post?                                   │
│  ┌──────────────────────────────────────────────────┐     │
│  │                                                  │     │
│  └──────────────────────────────────────────────────┘     │
│                                                           │
│                              [ Continuar ▶ ]             │
│                                                           │
└───────────────────────────────────────────────────────────┘
```

---

## Tarefas

### Backend

- [ ] Criar `WorkflowSessionManager.cs`:
  - `ConcurrentDictionary` de sessões ativas
  - Registro, resposta e limpeza de sessões
  - Background task de expiração (10 min)
- [ ] Estender `LinkedInWriterExecutor` para modo Consultado:
  - Gerar perguntas fixas por template
  - Chamar LLM para gerar até 3 perguntas dinâmicas
  - Pausar via `RequestPort`
  - Retomar com as respostas e gerar o post
- [ ] Criar `WorkflowRespondEndpoint.cs`: `POST /api/workflow/{sessionId}/respond`
- [ ] Registrar `WorkflowSessionManager` em `Program.cs` (singleton)

### Frontend

- [ ] Criar `ConsultedQuestionsComponent`:
  - Renderiza perguntas dinamicamente (array do evento SignalR)
  - Campo de texto livre por pergunta
  - Indicação "todas são opcionais"
  - Botão "Continuar" chama `POST /api/workflow/{sessionId}/respond`
- [ ] Atualizar `SignalRService` para detectar `awaiting_input` e exibir o componente
- [ ] Ocultar `ConsultedQuestionsComponent` após "Continuar"

---

## Critério de Conclusão

```
✅  Modo Consultado completo:
    URL → transcrição → resumo → perguntas exibidas
    → usuário responde → post gerado com contexto

✅  Usuário clica "Continuar" sem responder
    → post gerado apenas com o resumo

✅  Sessão expira em 10 min sem resposta
    → erro exibido na UI com mensagem de timeout

✅  Endpoint /respond retorna 404 para sessionId inválido
```

---

## Fora do Escopo desta Fase

- Edição das perguntas pelo usuário
- Mais de 3 perguntas dinâmicas
- Histórico de respostas
- Persistência das sessões entre restarts do servidor

