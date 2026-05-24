## Context

O projeto `youtube-to-linkedin` é uma aplicação .NET 10 (backend) + Angular 19 (frontend) que transforma vídeos do YouTube em posts para o LinkedIn. A Fase 1 estabeleceu o scaffold completo: endpoint REST (`POST /api/workflow/start`), hub SignalR (`WorkflowHub`) e serviços Angular (`SignalRService`, `WorkflowService`).

O estado atual do backend aceita a requisição mas não realiza nenhum processamento real sobre a URL recebida. Esta fase introduz o primeiro executor concreto do pipeline: o `TranscriptExecutor`.

A biblioteca escolhida é **YoutubeExplode 6.6.0** — gratuita, sem OAuth, sem conta Google, ~2.4M downloads, licença MIT. A extração de `videoId` será feita por Regex para cobrir os 3 formatos de URL mais comuns antes de qualquer chamada de rede.

## Goals / Non-Goals

**Goals:**
- Extrair transcrição em texto puro de vídeos públicos do YouTube
- Tratar erros mapeados (URL inválida, vídeo privado/removido, sem transcrição) com mensagens amigáveis
- Emitir eventos SignalR de progresso (`in_progress`, `completed`, `error`) por sessão
- Exibir progresso das etapas no frontend com suporte a retry

**Non-Goals:**
- Persistência da transcrição (sem banco de dados nesta fase)
- Suporte a vídeos privados via OAuth
- Tratamento especial de transcrições muito longas (truncagem, paginação)
- Implementação dos executores de resumo ou post (Fases 3 e 4)
- `WorkflowFactory` ou qualquer abstração de orquestração multi-executor

## Decisions

### D1 — `YoutubeExplode` como cliente YouTube

**Decisão:** Usar `YoutubeExplode 6.6.0` (NuGet).

**Alternativas consideradas:**
- YouTube Data API v3 — exige conta Google, OAuth, cota diária limitada; descartada pela complexidade de setup para MVP.
- `youtube-transcript-api` (Python) — inviável no stack .NET.
- Scraping direto do HTML do YouTube — frágil, quebraria a cada redesign.

**Justificativa:** YoutubeExplode abstrai todas as chamadas à API interna do YouTube sem necessidade de credenciais. Ativamente mantida e bem testada pela comunidade.

---

### D2 — Extração de `videoId` por Regex antes de chamar a API

**Decisão:** Validar e extrair o `videoId` por Regex cobrindo `watch?v=`, `youtu.be/` e `/shorts/` **antes** de qualquer chamada de rede.

**Alternativas consideradas:**
- Deixar YoutubeExplode lançar exceção para URLs inválidas — possível, mas mistura responsabilidades e a mensagem de erro seria genérica.

**Justificativa:** Falha rápida sem I/O; mensagem de erro específica para URL inválida; evita chamada desnecessária à API externa.

---

### D3 — `TranscriptExecutor` como classe simples injetada no endpoint

**Decisão:** `TranscriptExecutor` é uma classe de serviço registrada no DI container, chamada diretamente pelo `WorkflowStartEndpoint`. Nenhuma `WorkflowFactory` ou abstração de orquestrador nesta fase.

**Alternativas consideradas:**
- Criar `IWorkflowExecutor` + `WorkflowFactory` agora — prematura; há apenas um executor nesta fase.

**Justificativa:** YAGNI. A abstração será introduzida quando houver múltiplos executores para coordenar (Fases 3/4).

---

### D4 — Eventos SignalR por `sessionId`

**Decisão:** Cada evento SignalR usa o `sessionId` como identificador de grupo/conexão, garantindo que apenas o cliente correto receba as atualizações.

**Justificativa:** Mantém isolamento entre sessões concorrentes; alinhado com a arquitetura estabelecida na Fase 1.

---

### D5 — `WorkflowProgressComponent` com 3 etapas fixas

**Decisão:** O componente exibe exatamente 3 etapas: `transcript` (Fase 2), `summary` (Fase 3), `post` (Fase 4). As etapas de fases futuras são mostradas como `pendente` desde o início.

**Justificativa:** O usuário tem visibilidade do fluxo completo desde a Fase 2; facilita a evolução incremental das fases seguintes.

## Risks / Trade-offs

| Risco | Mitigação |
|---|---|
| YoutubeExplode pode quebrar se o YouTube mudar sua API interna | Biblioteca mantida ativamente; aceita-se o risco para MVP |
| Vídeos com legendas automáticas (não humanas) podem ter qualidade baixa | Fora do escopo do MVP; será tratado como conteúdo de baixa qualidade na geração do post |
| Transcrições muito longas podem exceder o contexto do LLM nas fases seguintes | Sem tratamento nesta fase; contexto longo do modelo cobre a maioria dos casos para MVP |
| Falha de rede ao chamar YoutubeExplode | Capturada pelo handler genérico (`Exception`) → mensagem: "Ocorreu um erro ao extrair a transcrição. Tente novamente." |
