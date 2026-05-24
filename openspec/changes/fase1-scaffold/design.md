# Design: Fase 1 — Scaffold e Setup do Projeto

## Architecture

```
youtube-to-linkedin/
├── src/
│   ├── backend/
│   │   └── YoutubeToLinkedIn.Api/
│   │       ├── Hubs/
│   │       │   └── WorkflowHub.cs
│   │       ├── Endpoints/
│   │       │   └── WorkflowStartEndpoint.cs
│   │       └── Program.cs
│   └── frontend/
│       └── youtube-to-linkedin-app/
│           └── src/app/
│               ├── services/
│               │   ├── signalr.service.ts
│               │   └── workflow.service.ts
│               └── app.component.ts
```

## Key Decisions

| # | Decision | Justification |
|---|---|---|
| R1 | .NET 10 runtime | Already installed; no extra setup |
| R2 | Angular standalone components | Angular 21 default; no NgModules needed |
| R3 | CORS only for `localhost:4200` in Development | Basic security; revisited in production |
| R4 | Backend on `https://localhost:5001` | .NET default HTTPS port |
| R5 | Frontend on `http://localhost:4200` | Angular CLI default |
| R6 | `sessionId` is a GUID generated on every POST | Unique, collision-free, no external state |
| R7 | All NuGets installed in this phase | Prevents build failures in later phases |
| R8 | All data is mocked | Goal is to validate transport plumbing |
| R9 | SignalR uses generic `SendProgress` method | Reused unchanged in all future phases |
| R10 | `WorkflowHub` has no business logic | Hub is transport only; logic goes in Executors |

## NuGet Packages

| Package | Version | Purpose |
|---|---|---|
| `Microsoft.Agents.AI` | prerelease | Agent Framework base |
| `Microsoft.Agents.AI.Workflows` | prerelease | Workflow graph and executors |
| `YoutubeExplode` | 6.6.0 | YouTube transcript and captions |
| `Azure.AI.OpenAI` | latest stable | LLM client |
| `Microsoft.AspNetCore.SignalR` | built-in .NET 10 | SignalR Hub (no extra install) |

## NPM Packages

| Package | Version | Purpose |
|---|---|---|
| `@microsoft/signalr` | latest | SignalR JS client |

## API Contract

### POST /api/workflow/start
**Request body**: `{ url: string, postType: string, mode: string }`  
**Response**: `{ sessionId: string }` (GUID)

### SignalR Hub: /hubs/workflow
**Server → Client method**: `SendProgress(sessionId, message)`  
After a POST, the backend broadcasts at least one `SendProgress` event to confirm the hub is live.

## Component Responsibilities

| Component | Responsibility |
|---|---|
| `WorkflowHub.cs` | Transport only — broadcast `SendProgress` to caller group |
| `WorkflowStartEndpoint.cs` | Generate GUID, trigger mock SignalR event, return sessionId |
| `Program.cs` | Wire up CORS, SignalR, Minimal API |
| `SignalRService` | Connect to hub, expose `progress$` Observable |
| `WorkflowService` | HTTP POST to `/api/workflow/start`, return sessionId |
| `AppComponent` | 3-field form (URL, postType, mode); call start(); log events |
