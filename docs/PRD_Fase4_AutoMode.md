# PRD — Fase 4: Geração do Post — Modo Auto (LinkedInWriterExecutor)

> **Versão:** 1.0
> **Data:** 2026-05-24
> **Depende de:** [PRD_Fase3_Summary.md](./PRD_Fase3_Summary.md)
> **Referência:** [IMPLEMENTATION_PLAN.md](./IMPLEMENTATION_PLAN.md)

---

## Objetivo

Dado o resumo estruturado e o tipo de post selecionado pelo usuário, gerar automaticamente um rascunho completo no formato correto, seguindo as regras de template, formatação, SEO e tom da skill `criar-post-linkedin`. O usuário **não é consultado** neste modo — a IA decide tudo.

---

## Fluxo da Fase

```
  SummaryExecutor
       │
       │  summary: string
       │  postType: "storytelling" | "lista" | "opiniao"
       │  mode: "auto"
       ▼
┌──────────────────────────────────────────────────────┐
│            LinkedInWriterExecutor (Auto)             │
│                                                      │
│  1. Emite SignalR { step: "writing", in_progress }   │
│  2. Monta prompt:                                    │
│     [system: linkedin-writer-system.md]              │
│     [user:   resumo + tipo de post]                  │
│  3. Chama Azure OpenAI                               │
│  4. Parseia resposta → { draft, templateUsed }       │
│  5. Emite SignalR { step: "writing", completed,      │
│                     result: PostDraftResult }        │
└──────────────────────────┬───────────────────────────┘
                           │
                           ▼
                    PostDraftResult
                  { draft: string,
                    templateUsed: string }
```

---

## Os 3 Templates

```
┌─────────────────────────────────────────────────────────────────┐
│                      TEMPLATES DE POST                          │
├────────────────┬────────────────────────────────────────────────┤
│ STORYTELLING   │ Hook pessoal → Contexto → Erro/Obstáculo →     │
│                │ Virada → Aprendizado → CTA                     │
├────────────────┼────────────────────────────────────────────────┤
│ LISTA PRÁTICA  │ Hook com número → Item 1...N (dica/erro/       │
│                │ ferramenta) → Insight principal → CTA          │
├────────────────┼────────────────────────────────────────────────┤
│ OPINIÃO        │ Hook provocativo → Crença comum (a refutar) →  │
│ PROVOCATIVA    │ Argumento + dados/exemplos → Visão alternativa  │
│                │ → CTA                                          │
└────────────────┴────────────────────────────────────────────────┘
```

---

## Regras do Prompt (`linkedin-writer-system.md`)

```
┌─────────────────────────────────────────────────────────────────┐
│                 linkedin-writer-system.md                       │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  FORMATAÇÃO                                                     │
│  • Parágrafos curtos (1–3 linhas)                               │
│  • Linha em branco entre parágrafos                             │
│  • Emojis: 0 a 2 por post (somente se naturais ao contexto)    │
│  • Hashtags: 3 a 5, ao final do post                           │
│                                                                 │
│  SEO E VISIBILIDADE                                             │
│  • Keyword principal no hook (primeira linha)                   │
│  • Nomes completos de tecnologias (não abreviações)            │
│  • 200 a 320 palavras no total                                 │
│                                                                 │
│  TOM E VOZ                                                      │
│  • Primeira pessoa ("Eu fiz", "Aprendi", "Descobri")           │
│  • Tom pessoal, direto, sem jargões corporativos               │
│  • Sem frases genéricas de abertura ("Hoje vou falar sobre")   │
│                                                                 │
│  ESTRUTURA                                                      │
│  • Seguir o template escolhido (informado no user message)     │
│  • Hook forte na primeira linha (gera curiosidade ou tensão)   │
│  • CTA ao final (pergunta ou convite à interação)              │
│                                                                 │
│  SAÍDA ESPERADA                                                 │
│  Retornar JSON:                                                 │
│  {                                                              │
│    "draft": "texto completo do post",                          │
│    "templateUsed": "Storytelling | Lista Prática |             │
│                     Opinião Provocativa"                       │
│  }                                                              │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## Modelo `PostDraftResult`

```csharp
// Models/PostDraftResult.cs
public record PostDraftResult(
    string Draft,
    string TemplateUsed
);
```

---

## Eventos SignalR desta Fase

| Evento | Payload | Quando |
|---|---|---|
| `workflowEvent` | `{ step: "writing", status: "in_progress" }` | Antes de chamar o LLM |
| `workflowEvent` | `{ step: "writing", status: "completed", result: PostDraftResult }` | Post gerado |
| `workflowEvent` | `{ step: "writing", status: "error", message: "..." }` | Falha no LLM |

---

## Regras e Decisões

| # | Regra / Decisão | Justificativa |
|---|---|---|
| R1 | Prompt em **`Prompts/linkedin-writer-system.md`** | Mesmo padrão do SummaryExecutor; editável sem recompilar |
| R2 | LLM retorna **JSON estruturado** `{ draft, templateUsed }` | Facilita parse e exibição separada do template usado |
| R3 | Modo Auto: executor **não emite `awaiting_input`** | Responsabilidade única; modo Consultado é Fase 5 |
| R4 | `postType` é passado no **user message**, não no system prompt | Permite reuso do mesmo system prompt para todos os tipos |
| R5 | Tamanho: **200–320 palavras** | Regra da skill `criar-post-linkedin` |
| R6 | Hashtags: **3–5**, sempre ao final | Regra da skill; SEO do LinkedIn |
| R7 | Emojis: **0–2** por post | Regra da skill; evita aparência spam |
| R8 | Keyword no **hook (primeira linha)** | Regra de SEO da skill |
| R9 | Tom: **primeira pessoa**, pessoal e direto | Regra da skill; autenticidade no LinkedIn |
| R10 | `templateUsed` é informado pela IA no JSON | Exibido na UI para transparência ao usuário |

---

## UI — PostDraftComponent

```
┌──────────────────────────────────────────────────────┐
│                  PostDraftComponent                  │
├──────────────────────────────────────────────────────┤
│                                                      │
│  ✅  Extraindo transcrição do vídeo                  │
│  ✅  Resumindo conteúdo                              │
│  ✅  Gerando rascunho do post                        │
│                                                      │
├──────────────────────────────────────────────────────┤
│                                                      │
│  📝  Template utilizado: Lista Prática               │
│                                                      │
│  ┌────────────────────────────────────────────────┐  │
│  │  5 erros que cometi ao usar IA para escrever   │  │
│  │  código em produção.                           │  │
│  │                                                │  │
│  │  Erro 1: confiar no output sem testar...       │  │
│  │                                                │  │
│  │  ...                                           │  │
│  │                                                │  │
│  │  #InteligenciaArtificial #Dev #Produção        │  │
│  └────────────────────────────────────────────────┘  │
│                                                      │
│  [ Copiar ]              [ Gerar Novo Post ]         │
│                                                      │
└──────────────────────────────────────────────────────┘
```

---

## Tarefas

### Backend

- [ ] Criar `Prompts/linkedin-writer-system.md` com todas as regras da skill
- [ ] Criar `PostDraftResult.cs` (record com `Draft` e `TemplateUsed`)
- [ ] Criar `LinkedInWriterExecutor.cs` — modo Auto:
  - Recebe resumo + tipo de post
  - Monta prompt e chama Azure OpenAI
  - Parseia JSON de resposta → `PostDraftResult`
  - Emite eventos SignalR
- [ ] Encadear `LinkedInWriterExecutor` após `SummaryExecutor` no `WorkflowStartEndpoint` (sem WorkflowFactory)

### Frontend

- [ ] Criar `PostDraftComponent`:
  - Exibe `templateUsed`
  - Exibe `draft` com formatação (espaçamento entre parágrafos)
  - Botão "Copiar" com feedback visual "Copiado!" (2s)
  - Botão "Gerar Novo Post" → reset completo do estado

---

## Critério de Conclusão

```
✅  Fluxo completo modo Auto:
    URL → transcrição → resumo → post gerado → exibido na UI

✅  PostDraftComponent exibe:
    - template utilizado
    - rascunho formatado
    - botão "Copiar" funcional

✅  "Gerar Novo Post" reseta todos os estados
```

---

## Fora do Escopo desta Fase

- Modo Consultado (perguntas ao usuário) — ver Fase 5
- Edição do rascunho na UI
- Histórico de posts gerados
- Regeneração com prompt diferente

