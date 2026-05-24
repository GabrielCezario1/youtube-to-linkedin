# PRD — Fase 1: Scaffold e Setup do Projeto

> **Versão:** 1.0
> **Data:** 2026-05-24
> **Referência:** [IMPLEMENTATION_PLAN.md](./IMPLEMENTATION_PLAN.md) · [PRD_TechContentAgent.md](./PRD_TechContentAgent.md)

---

## Objetivo

Criar a estrutura da solução do zero, com o backend compilando, o frontend inicializado e a comunicação SignalR funcionando ponta a ponta — mesmo com dados mockados.

---

## Fluxo da Fase

```
Desenvolvedor
     │
     ▼
┌────────────────────────────────────────────┐
│         Criar Solução (.sln + API)         │
│  - YoutubeToLinkedIn.Api (.NET 10)         │
│  - NuGets instalados                       │
│  - Program.cs configurado                  │
└──────────────────┬─────────────────────────┘
                   │
                   ▼
┌────────────────────────────────────────────┐
│         Endpoints e SignalR Hub            │
│  POST /api/workflow/start → sessionId mock │
│  Hub /hubs/workflow → broadcast evento     │
└──────────────────┬─────────────────────────┘
                   │
                   ▼
┌────────────────────────────────────────────┐
│         Projeto Angular                    │
│  - @microsoft/signalr instalado            │
│  - SignalRService conectado ao hub         │
│  - WorkflowService.start() chamando POST   │
│  - AppComponent com formulário básico      │
└──────────────────┬─────────────────────────┘
                   │
                   ▼
          ✅ Critério de conclusão:
     POST → sessionId retornado
     Frontend recebe ≥ 1 evento SignalR
```

---

## Arquitetura do Scaffold

```
youtube-to-linkedin/
├── src/
│   ├── backend/
│   │   └── YoutubeToLinkedIn.Api/
│   │       ├── Hubs/
│   │       │   └── WorkflowHub.cs          ← broadcast básico
│   │       ├── Endpoints/
│   │       │   └── WorkflowStartEndpoint.cs ← retorna sessionId mockado
│   │       └── Program.cs                  ← CORS + SignalR + Minimal API
│   │
│   └── frontend/
│       └── youtube-to-linkedin-app/
│           └── src/app/
│               ├── services/
│               │   ├── signalr.service.ts  ← conecta ao hub
│               │   └── workflow.service.ts ← chama POST /start
│               └── app.component.ts        ← formulário básico
```

---

## Stack e Pacotes

### Backend

| Pacote | Versão | Finalidade |
|---|---|---|
| `Microsoft.Agents.AI` | prerelease | Agent Framework — base |
| `Microsoft.Agents.AI.Workflows` | prerelease | Workflow graph e executors |
| `YoutubeExplode` | 6.6.0 | Extração de transcrição e legendas do YouTube (instalado já nesta fase) |
| `Azure.AI.OpenAI` | latest | Cliente LLM (instalado já nesta fase) |
| `Microsoft.AspNetCore.SignalR` | built-in .NET 10 | SignalR Hub |

### Frontend

| Pacote | Versão | Finalidade |
|---|---|---|
| `@microsoft/signalr` | latest | Cliente SignalR |
| `@angular/core` | 17+ (v21.x) | Framework |

---

## Regras e Decisões

| # | Regra / Decisão | Justificativa |
|---|---|---|
| R1 | Runtime: **.NET 10** | Já instalado na máquina; nenhuma instalação adicional necessária |
| R2 | Framework frontend: **Angular standalone components** | Versão atual (21.x) usa standalone por padrão |
| R3 | CORS liberado para `localhost:4200` apenas em Development | Segurança básica; em produção será revisado |
| R4 | Porta backend: `https://localhost:5001` | Padrão .NET |
| R5 | Porta frontend: `http://localhost:4200` | Padrão Angular CLI |
| R6 | `sessionId` é um **GUID** gerado no backend ao receber `/start` | Único, sem colisão, sem estado externo |
| R7 | Todos os NuGets são instalados nesta fase | Evita problemas de build nas fases seguintes |
| R8 | Dados desta fase são **mockados** (sem lógica real) | O objetivo é validar a plumbing, não o comportamento |
| R9 | SignalR Hub usa **método de broadcast genérico** `SendProgress` | Reaproveitado nas fases seguintes sem alteração de contrato |
| R10 | `WorkflowHub.cs` não implementa lógica de negócio | Hub é apenas transporte; lógica fica nos Executors |

---

## Tarefas

### Backend

- [ ] Criar solution `.sln` e projeto `YoutubeToLinkedIn.Api`
- [ ] Instalar todos os NuGets listados acima
- [ ] Configurar `Program.cs`: CORS, SignalR, Minimal API
- [ ] Criar `WorkflowHub.cs` com método `SendProgress` básico
- [ ] Criar `WorkflowStartEndpoint.cs`: `POST /api/workflow/start` retorna `{ sessionId: Guid }`
- [ ] Verificar que o projeto compila sem erros

### Frontend

- [ ] Criar projeto Angular com Angular CLI (`ng new`)
- [ ] Instalar `@microsoft/signalr`
- [ ] Criar `SignalRService`: conecta ao hub e expõe Observable de eventos
- [ ] Criar `WorkflowService`: método `start(url, postType, mode)` chama `POST /api/workflow/start`
- [ ] Criar `AppComponent` com formulário básico (3 campos)
- [ ] Verificar que o projeto compila sem erros

---

## Critério de Conclusão

```
✅  POST /api/workflow/start
    └── retorna { sessionId: "guid-aqui" }

✅  SignalR Hub emite ao menos 1 evento após o POST
    └── Frontend exibe o evento no console do browser

✅  Nenhum erro de compilação em backend ou frontend
```

---

## Fora do Escopo desta Fase

- Lógica de transcrição, resumo ou geração de post
- Validação de URL
- Tratamento de erros
- UI além do formulário básico

