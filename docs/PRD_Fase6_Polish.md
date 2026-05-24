# PRD — Fase 6: Polimento e Tratamento de Erros

> **Versão:** 1.0
> **Data:** 2026-05-24
> **Depende de:** [PRD_Fase4_AutoMode.md](./PRD_Fase4_AutoMode.md) · [PRD_Fase5_ConsultedMode.md](./PRD_Fase5_ConsultedMode.md)
> **Referência:** [IMPLEMENTATION_PLAN.md](./IMPLEMENTATION_PLAN.md)

---

## Objetivo

Garantir que todos os cenários de erro descritos no PRD principal estão cobertos, e que a UX é consistente e robusta tanto no happy path quanto nos error paths. Nenhuma nova funcionalidade é adicionada nesta fase — apenas solidez.

---

## Mapa Completo de Erros

```
┌─────────────────────────────────────────────────────────────────────┐
│                    CENÁRIOS DE ERRO — COBERTURA                     │
├──────────────────────────┬──────────────────────────────────────────┤
│  ORIGEM                  │  TRATAMENTO                              │
├──────────────────────────┼──────────────────────────────────────────┤
│  URL vazia / inválida    │  Validação no endpoint /start antes      │
│                          │  de iniciar workflow                     │
├──────────────────────────┼──────────────────────────────────────────┤
│  Vídeo privado /         │  Mensagem: "Não foi possível acessar     │
│  removido                │  este vídeo. Verifique se é público."    │
│                          │  + botão Tentar Novamente (URL mantida)  │
├──────────────────────────┼──────────────────────────────────────────┤
│  Vídeo sem transcrição   │  Mensagem: "Este vídeo não possui        │
│                          │  transcrição disponível."                │
│                          │  + botão "Tentar com outro vídeo"        │
│                          │  (URL limpa)                             │
├──────────────────────────┼──────────────────────────────────────────┤
│  Falha no LLM            │  Mensagem: "Ocorreu um erro ao           │
│  (summary ou writing)    │  processar o conteúdo. Tente novamente." │
│                          │  + botão Tentar Novamente                │
├──────────────────────────┼──────────────────────────────────────────┤
│  Timeout modo consultado │  Mensagem: "Sessão expirada.             │
│  (10 min sem resposta)   │  Inicie novamente."                      │
│                          │  + retorna ao formulário                 │
├──────────────────────────┼──────────────────────────────────────────┤
│  Cancelamento pelo       │  Workflow interrompido imediatamente     │
│  usuário                 │  + formulário restaurado com dados       │
└──────────────────────────┴──────────────────────────────────────────┘
```

---

## Fluxo de Retry por Tipo de Erro

```
         Erro na etapa "transcript"
                    │
         ┌──────────┴──────────┐
         ▼                     ▼
   Vídeo privado         Sem transcrição
         │                     │
         ▼                     ▼
  [ Tentar Novamente ]   [ Tentar com outro vídeo ]
  URL preservada         URL limpa
         │                     │
         └──────────┬──────────┘
                    ▼
           Formulário habilitado
           Dados preservados
           (exceto URL se sem transcrição)


         Erro na etapa "summary" ou "writing"
                    │
                    ▼
           [ Tentar Novamente ]
           Workflow reinicia da etapa com falha
           Todos os dados do formulário preservados
```

---

## Comportamentos de UX a Garantir

```
┌─────────────────────────────────────────────────────────────────────┐
│                      CHECKLIST DE UX                                │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  FORMULÁRIO                                                         │
│  [ ] Bloqueado durante todo o processamento                         │
│  [ ] Botão "Gerar Post" desabilitado durante processamento          │
│  [ ] Retry preserva URL, tipo de post e modo selecionados           │
│  [ ] Retry por "sem transcrição" limpa apenas o campo URL           │
│  [ ] Retry chama novo POST — frontend sobrescreve o sessionId ativo  │
│                                                                     │
│  CANCELAMENTO                                                       │
│  [ ] Botão "Cancelar" visível durante qualquer etapa                │
│  [ ] Cancelamento interrompe o workflow no backend                  │
│  [ ] Formulário restaurado com todos os valores anteriores          │
│                                                                     │
│  RESET COMPLETO                                                     │
│  [ ] "Gerar Novo Post" limpa todos os estados e campos              │
│  [ ] Nenhum dado da sessão anterior persiste na UI                  │
│                                                                     │
│  RESPONSIVIDADE                                                     │
│  [ ] Layout funcional em mobile (breakpoint 375px)                  │
│  [ ] Layout funcional em tablet (breakpoint 768px)                  │
│  [ ] Layout funcional em desktop (breakpoint 1280px)                │
│                                                                     │
│  FEEDBACK VISUAL                                                    │
│  [ ] Botão "Copiar" exibe "Copiado!" por 2 segundos                 │
│  [ ] Estados das etapas: ○ pendente / ⏳ andamento / ✅ ok / ❌ erro │
│  [ ] Indicação de carregamento durante chamadas HTTP                │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Validação do Endpoint `/start` (backend)

```
POST /api/workflow/start
{
  "url":      string,   ← obrigatório, deve ser URL YouTube válida
  "postType": string,   ← obrigatório: "storytelling" | "lista" | "opiniao"
  "mode":     string    ← obrigatório: "auto" | "consultado"
}

Validações antes de iniciar o workflow:
┌──────────────────────────────────────────────────────┐
│  url vazia          → 400 "URL é obrigatória"        │
│  url não YouTube    → 400 "URL inválida"             │
│  postType inválido  → 400 "Tipo de post inválido"    │
│  mode inválido      → 400 "Modo inválido"            │
└──────────────────────────────────────────────────────┘
```

---

## Regras e Decisões

| # | Regra / Decisão | Justificativa |
|---|---|---|
| R1 | Validação de URL no **endpoint antes** de iniciar o workflow | Falha rápida; evita criar sessão e executors sem necessidade |
| R2 | Retry de "vídeo privado" **preserva a URL** | Usuário pode querer tentar novamente depois |
| R3 | Retry de "sem transcrição" **limpa a URL** | Não faz sentido tentar o mesmo vídeo; sinaliza troca |
| R4 | Cancelamento **interrompe o CancellationToken** passado ao workflow | Mecanismo nativo de cancelamento do Agent Framework |
| R5 | Formulário **bloqueado** (não apenas botão) durante processamento | Evita alteração acidental de dados enquanto o workflow roda |
| R6 | Responsividade: **breakpoints 375px, 768px, 1280px** | Cobre mobile, tablet e desktop sem frameworks extras |
| R7 | Erros do backend **não expõem stack trace** ao frontend | Segurança básica; mensagens amigáveis ao usuário |
| R8 | Reset ("Gerar Novo Post") limpa **estado do componente Angular**, não recarrega a página | SPA; sem perda de conexão SignalR |
| R9 | Cancelamento deve funcionar durante **qualquer etapa** incluindo a pausa do modo Consultado | Consistência com o PRD principal |
| R10 | Timeout de sessão consultada emite **evento SignalR de erro** antes de limpar da memória | UI precisa saber que a sessão expirou |

---

## Tarefas

### Backend

- [ ] Adicionar validação de URL em `WorkflowStartEndpoint.cs`
- [ ] Adicionar validação de `postType` e `mode`
- [ ] Implementar cancelamento via `CancellationToken` nos executors
- [ ] Garantir que mensagens de erro são amigáveis (sem stack trace)
- [ ] Testar timeout de sessão consultada (10 min → evento de erro SignalR)

### Frontend

- [ ] Bloquear formulário completo (não apenas botão) durante processamento
- [ ] Desabilitar botão "Gerar Post" durante processamento
- [ ] Implementar botão "Cancelar" funcional em todas as etapas
- [ ] Garantir que retry preserva dados do formulário por tipo de erro
- [ ] Garantir que "Gerar Novo Post" limpa todos os estados
- [ ] Implementar responsividade básica (3 breakpoints)
- [ ] Testar e corrigir todos os error paths do PRD principal

---

## Critério de Conclusão

```
✅  Todos os cenários de erro do PRD principal cobertos e testados

✅  Formulário bloqueado durante processamento em todos os estados

✅  Cancelamento funcional em todas as etapas, incluindo modo Consultado

✅  Retry correto por tipo de erro:
    - privado/genérico → URL preservada
    - sem transcrição → URL limpa

✅  Layout responsivo nos 3 breakpoints

✅  Happy path completo sem erros de console
```

---

## Fora do Escopo desta Fase

- Testes automatizados (unitários, e2e)
- Monitoramento e logging em produção
- Internacionalização
- Animações elaboradas de transição

