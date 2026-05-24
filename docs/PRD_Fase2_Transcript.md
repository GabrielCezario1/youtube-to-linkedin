# PRD — Fase 2: Extração de Transcrição (TranscriptExecutor)

> **Versão:** 1.0
> **Data:** 2026-05-24
> **Depende de:** [PRD_Fase1_Scaffold.md](./PRD_Fase1_Scaffold.md)
> **Referência:** [IMPLEMENTATION_PLAN.md](./IMPLEMENTATION_PLAN.md)

---

## Objetivo

Dado uma URL do YouTube, extrair e retornar a transcrição em texto puro, reportando progresso em tempo real via SignalR e tratando todos os cenários de erro descritos no PRD principal.

---

## Fluxo da Fase

```
  Frontend                   Backend                  YouTube
     │                          │                        │
     │  POST /api/workflow/start │                        │
     │─────────────────────────▶│                        │
     │                          │  extrai videoId da URL │
     │                          │───────────────────────▶│
     │◀────────────────────────-│                        │
     │  SignalR: workflowEvent   │                        │
     │  { status: in_progress } │  YoutubeExplode        │
     │                          │───────────────────────▶│
     │                          │◀───────────────────────│
     │                          │  transcrição em texto   │
     │◀─────────────────────────│                        │
     │  SignalR: workflowEvent   │                        │
     │  { status: completed }   │                        │
```

---

## Formatos de URL Suportados

```
┌───────────────────────────────────────────────────────────┐
│                  URL PARSER — videoId                     │
├───────────────────────────────────────────────────────────┤
│                                                           │
│  youtube.com/watch?v=XXXXXXXXXXX  →  videoId: XXXXXXXXXXX │
│  youtu.be/XXXXXXXXXXX             →  videoId: XXXXXXXXXXX │
│  youtube.com/shorts/XXXXXXXXXXX   →  videoId: XXXXXXXXXXX │
│                                                           │
│  Qualquer outro formato           →  erro de URL inválida │
│                                                           │
└───────────────────────────────────────────────────────────┘
```

---

## Mapeamento de Erros

```
                     Chamada YoutubeExplode
                               │
              ┌────────────────┼────────────────┐
              ▼                ▼                ▼
         Vídeo OK        Vídeo privado    Sem transcrição
              │           ou removido           │
              ▼                │                ▼
    transcrição extraída       ▼         erro: "Este vídeo não
              │          erro: "Não foi  possui transcrição"
              ▼          possível acessar     │
     SignalR completed    este vídeo"         ▼
                               │        SignalR error
                               ▼        msg: sem_transcricao
                         SignalR error
                         msg: privado
```

---

## Eventos SignalR desta Fase

| Evento       | Payload                                                   | Quando               |
| ------------ | --------------------------------------------------------- | -------------------- |
| `workflowEvent` | `{ step: "transcript", status: "in_progress" }`           | Ao iniciar extração  |
| `workflowEvent` | `{ step: "transcript", status: "completed" }`             | Transcrição extraída |
| `workflowEvent` | `{ step: "transcript", status: "error", message: "..." }` | Qualquer falha       |

---

## Regras e Decisões

| #   | Regra / Decisão                                                                                                           | Justificativa                                                             |
| --- | ------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------- |
| R1  | Usar **`YoutubeExplode`** (NuGet, 6.6.0)                                                                                  | Gratuito, sem OAuth, sem conta Google, 2.4M downloads, licença MIT        |
| R2  | Extração de `videoId` via **Regex** cobrindo os 3 formatos                                                                | Cobre todos os formatos comuns do YouTube                                 |
| R3  | URL inválida (sem videoId extraível) → **erro antes de chamar a API**                                                     | Falha rápida; evita chamada desnecessária                                 |
| R4  | Vídeo privado/removido → mensagem: `"Não foi possível acessar este vídeo. Verifique se ele é público e tente novamente."` | Mensagem do PRD principal, sem expor detalhes internos                    |
| R5  | Vídeo sem transcrição → mensagem: `"Este vídeo não possui transcrição disponível. Tente com outro vídeo."`                | Mensagem do PRD principal                                                 |
| R6  | Falha genérica da API → mensagem: `"Ocorreu um erro ao extrair a transcrição. Tente novamente."`                          | Não expõe stack trace ao usuário                                          |
| R7  | A transcrição **não é persistida** — trafega apenas em memória dentro da sessão                                           | MVP sem banco de dados                                                    |
| R8  | `TranscriptExecutor` não conhece o modo (Auto/Consultado)                                                                 | Responsabilidade única; o modo é tratado no `LinkedInWriterExecutor`      |
| R9  | SignalR emite evento `in_progress` **antes** de chamar a API externa                                                      | UX: usuário vê feedback imediato                                          |
| R10 | Transcrições muito longas **não recebem tratamento especial** nesta fase                                                  | Fora do escopo do MVP; contexto longo do modelo cobre a maioria dos casos |

---

## Interface do TranscriptExecutor

```
┌─────────────────────────────────────────────────┐
│               TranscriptExecutor                │
├─────────────────────────────────────────────────┤
│  Input:                                         │
│    url: string          (URL do YouTube)        │
│    sessionId: string    (para emitir SignalR)   │
│                                                 │
│  Output:                                        │
│    transcript: string   (texto puro)            │
│                                                 │
│  Exceções mapeadas:                             │
│    VideoUnavailableException  → msg: privado    │
│    !manifest.Tracks.Any()     → msg: sem legenda│
│    Exception                  → msg: genérica   │
└─────────────────────────────────────────────────┘
```

---

## UI — WorkflowProgressComponent

```
┌──────────────────────────────────────────────────┐
│  Estados visuais das etapas                      │
├──────────────────────────────────────────────────┤
│                                                  │
│  ⏳  Extraindo transcrição do vídeo...            │  ← in_progress
│  ○   Resumindo conteúdo                          │  ← pendente
│  ○   Gerando rascunho do post                    │  ← pendente
│                                                  │
├──────────────────────────────────────────────────┤
│  Após conclusão:                                 │
│                                                  │
│  ✅  Extraindo transcrição do vídeo              │  ← completed
│  ⏳  Resumindo conteúdo...                       │  ← in_progress (Fase 3)
│  ○   Gerando rascunho do post                    │  ← pendente
│                                                  │
├──────────────────────────────────────────────────┤
│  Em caso de erro:                                │
│                                                  │
│  ❌  Extraindo transcrição do vídeo              │  ← error
│                                                  │
│  ┌────────────────────────────────────────────┐  │
│  │ ⚠️  Este vídeo não possui transcrição      │  │
│  │     disponível. Tente com outro vídeo.     │  │
│  │                                            │  │
│  │           [ Tentar com outro vídeo ]       │  │
│  └────────────────────────────────────────────┘  │
│                                                  │
└──────────────────────────────────────────────────┘
```

---

## Tarefas

### Backend

- [ ] Criar `TranscriptExecutor.cs` com `YoutubeExplode`
- [ ] Implementar extração de `videoId` via Regex (3 formatos)
- [ ] Tratar exceções mapeadas e emitir mensagens corretas
- [ ] Emitir eventos SignalR: `in_progress`, `completed`, `error`
- [ ] Chamar `TranscriptExecutor` diretamente no `WorkflowStartEndpoint` (sem WorkflowFactory)

### Frontend

- [ ] Criar `WorkflowProgressComponent` com 3 etapas
- [ ] Mapear estados: pendente / in_progress / completed / error
- [ ] Criar `ErrorDisplayComponent`: mensagem descritiva + botão de retry
- [ ] Retry preserva dados do formulário (URL, tipo, modo)

---

## Critério de Conclusão

```
✅  URL válida com transcrição disponível
    └── transcrição extraída → etapa "completed" na UI

✅  URL de vídeo privado
    └── etapa "error" com mensagem correta + botão retry

✅  URL de vídeo sem transcrição
    └── etapa "error" com mensagem correta + botão "outro vídeo"

✅  URL inválida (não é YouTube)
    └── erro antes de chamar API + mensagem descritiva
```

---

## Fora do Escopo desta Fase

- Geração de resumo ou post
- Tratamento de transcrições muito longas
- Suporte a vídeos privados via OAuth
- Paginação ou truncagem de transcrição
