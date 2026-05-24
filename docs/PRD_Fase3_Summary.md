# PRD — Fase 3: Resumo do Conteúdo (SummaryExecutor)

> **Versão:** 1.0
> **Data:** 2026-05-24
> **Depende de:** [PRD_Fase2_Transcript.md](./PRD_Fase2_Transcript.md)
> **Referência:** [IMPLEMENTATION_PLAN.md](./IMPLEMENTATION_PLAN.md)

---

## Objetivo

Dado a transcrição bruta do vídeo, chamar o LLM (Azure OpenAI) para gerar um resumo estruturado com 5–8 pontos-chave relevantes para um post técnico no LinkedIn.

---

## Fluxo da Fase

```
  TranscriptExecutor
         │
         │  transcript: string
         ▼
┌─────────────────────────────────────┐
│          SummaryExecutor            │
│                                     │
│  1. Emite SignalR in_progress        │
│  2. Monta prompt:                   │
│     [system: summarizer-system.md]  │
│     [user:   transcrição]           │
│  3. Chama Azure OpenAI              │
│  4. Parseia resposta                │
│  5. Emite SignalR completed         │
└──────────────┬──────────────────────┘
               │
               │  summary: string (lista numerada)
               ▼
      LinkedInWriterExecutor
          (Fases 4 e 5)
```

---

## Estrutura do Prompt (`summarizer-system.md`)

```
┌─────────────────────────────────────────────────────────────┐
│                  summarizer-system.md                       │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  INSTRUÇÃO PRINCIPAL                                        │
│  Você recebe a transcrição de um vídeo técnico do YouTube.  │
│  Extraia entre 5 e 8 pontos-chave relevantes para um post  │
│  técnico no LinkedIn.                                       │
│                                                             │
│  FORMATO DE SAÍDA (obrigatório)                             │
│  Lista numerada onde cada item tem:                         │
│  - Título curto (máx 8 palavras)                            │
│  - 1 linha de contexto explicando o ponto                   │
│                                                             │
│  Exemplo de saída:                                          │
│  1. Título do ponto-chave                                   │
│     Contexto: uma linha explicando o ponto.                 │
│                                                             │
│  RESTRIÇÕES                                                 │
│  - Foco em conteúdo técnico aplicável                       │
│  - Ignorar introduções, patrocinadores, canais              │
│  - Não inventar conteúdo que não está na transcrição        │
│  - Responder SOMENTE a lista, sem preâmbulo                 │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## Configuração do LLM

```
┌───────────────────────────────────────────────────┐
│                Azure OpenAI                       │
├───────────────────────────────────────────────────┤
│  Modelo:       gpt-4o ou gpt-4o-mini              │
│  Temperature:  0.3  (respostas consistentes)      │
│  Max tokens:   1200 (suficiente para 8 pontos)    │
│  Provider:     Azure OpenAI                       │
│  Config:       appsettings.json                   │
│                AzureOpenAI:Endpoint               │
│                AzureOpenAI:ApiKey                 │
│                AzureOpenAI:ModelId                │
└───────────────────────────────────────────────────┘
```

---

## Eventos SignalR desta Fase

| Evento | Payload | Quando |
|---|---|---|
| `summary` | `{ step: "summary", status: "in_progress" }` | Antes de chamar o LLM |
| `summary` | `{ step: "summary", status: "completed" }` | Resumo gerado |
| `summary` | `{ step: "summary", status: "error", message: "..." }` | Falha no LLM |

---

## Regras e Decisões

| # | Regra / Decisão | Justificativa |
|---|---|---|
| R1 | Provider: **Azure OpenAI** | Decisão tomada na exploração; suporte nativo no Agent Framework |
| R2 | Modelo: **gpt-4o-mini** por padrão (configurável) | Balanceia custo e qualidade para resumo |
| R3 | Prompt armazenado em **`Prompts/summarizer-system.md`** | Editável sem recompilar; facilita iteração |
| R4 | Prompt carregado no **startup** da aplicação | Evita I/O repetido em cada requisição |
| R5 | Saída esperada: **lista numerada** com título + contexto | Formato consumível pelo `LinkedInWriterExecutor` |
| R6 | **5 a 8 pontos-chave** extraídos | Suficiente para um post rico sem sobrecarregar |
| R7 | Falha do LLM → mensagem: `"Ocorreu um erro ao processar o conteúdo. Tente novamente."` | Não expõe detalhes da API ao usuário |
| R8 | O resumo **não é exibido na UI** — trafega apenas entre executors | PRD principal não prevê exibição do resumo |
| R9 | `SummaryExecutor` **não conhece o modo** (Auto/Consultado) | Responsabilidade única |
| R10 | Transcrições longas: o modelo usa o contexto total | Sem truncagem no MVP; monitorar em uso real |

---

## Interface do SummaryExecutor

```
┌─────────────────────────────────────────────────┐
│               SummaryExecutor                   │
├─────────────────────────────────────────────────┤
│  Input:                                         │
│    transcript: string   (texto puro)            │
│    sessionId:  string   (para emitir SignalR)   │
│                                                 │
│  Output:                                        │
│    summary: string      (lista numerada)        │
│                                                 │
│  Exceções mapeadas:                             │
│    RequestFailedException  → msg: genérica      │
│    TaskCanceledException   → msg: timeout       │
│    Exception               → msg: genérica      │
└─────────────────────────────────────────────────┘
```

---

## UI — WorkflowProgressComponent (atualização)

```
┌──────────────────────────────────────────────────┐
│  Progresso após Fase 2 concluída                 │
├──────────────────────────────────────────────────┤
│                                                  │
│  ✅  Extraindo transcrição do vídeo              │
│  ⏳  Resumindo conteúdo...                       │  ← novo estado
│  ○   Gerando rascunho do post                    │
│                                                  │
├──────────────────────────────────────────────────┤
│  Após conclusão:                                 │
│                                                  │
│  ✅  Extraindo transcrição do vídeo              │
│  ✅  Resumindo conteúdo                          │
│  ⏳  Gerando rascunho do post...  (Fases 4 e 5)  │
│                                                  │
└──────────────────────────────────────────────────┘
```

---

## Tarefas

### Backend

- [ ] Configurar cliente Azure OpenAI em `Program.cs` (injeção de dependência)
- [ ] Criar arquivo `Prompts/summarizer-system.md` com instrução e formato de saída
- [ ] Implementar carregamento dos prompts no startup
- [ ] Criar `SummaryExecutor.cs`:
  - Monta prompt (system + user com transcrição)
  - Chama Azure OpenAI
  - Retorna resposta como string
  - Trata exceções e emite eventos SignalR
- [ ] Integrar `SummaryExecutor` ao `WorkflowFactory` (após `TranscriptExecutor`)

### Frontend

- [ ] Atualizar `WorkflowProgressComponent` para refletir etapa "summary"

---

## Critério de Conclusão

```
✅  Transcrição válida recebida pelo executor
    └── LLM chamado → resumo com 5–8 pontos retornado
    └── etapa "Resumindo conteúdo" → completed na UI

✅  Falha no LLM
    └── etapa "Resumindo conteúdo" → error na UI
    └── mensagem genérica exibida + botão retry

✅  Prompt carregado do arquivo .md (não hardcoded)
```

---

## Fora do Escopo desta Fase

- Exibir o resumo para o usuário
- Geração do post final
- Modo Consultado
- Ajuste fino do prompt (será iterado em uso real)

