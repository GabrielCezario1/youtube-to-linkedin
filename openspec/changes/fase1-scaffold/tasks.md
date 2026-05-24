# Tasks: Fase 1 — Scaffold e Setup do Projeto

## Backend

- [x] Criar solution `.sln` e projeto `YoutubeToLinkedIn.Api`
- [x] Instalar todos os NuGets: Microsoft.Agents.AI, Microsoft.Agents.AI.Workflows, YoutubeExplode, Azure.AI.OpenAI
- [x] Configurar `Program.cs`: CORS (localhost:4200), SignalR, Minimal API
- [x] Criar `WorkflowHub.cs` com método `SendProgress` básico
- [x] Criar `WorkflowStartEndpoint.cs`: POST /api/workflow/start retorna { sessionId: Guid }
- [x] Verificar que o projeto backend compila sem erros

## Frontend

- [x] Criar projeto Angular com Angular CLI (ng new)
- [x] Instalar @microsoft/signalr
- [x] Criar `SignalRService`: conecta ao hub e expõe Observable de eventos
- [x] Criar `WorkflowService`: método start() chama POST /api/workflow/start
- [x] Criar `AppComponent` com formulário básico (3 campos: url, postType, mode)
- [x] Verificar que o projeto frontend compila sem erros
