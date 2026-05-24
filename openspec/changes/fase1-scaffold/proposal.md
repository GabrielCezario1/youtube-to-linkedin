# Proposal: Fase 1 — Scaffold e Setup do Projeto

## Summary

Create the solution from scratch with the backend compiling, the frontend initialized, and SignalR communication working end-to-end — even with mocked data.

## Problem

The project has no code yet. We need the foundational plumbing: a .NET 10 API with SignalR, and an Angular 21 frontend that connects to it, so all subsequent phases can build on top.

## Goals

- Create `YoutubeToLinkedIn.Api` (.NET 10) with SignalR hub and Minimal API endpoint
- Create Angular standalone app with SignalR client service
- Validate end-to-end communication: POST → sessionId returned, frontend receives ≥ 1 SignalR event
- Install all required NuGet and npm packages upfront to avoid blockers in later phases

## Non-Goals

- Transcript extraction logic
- Summary or LinkedIn post generation
- URL validation or error handling
- Production-ready UI

## Approach

### Backend
- `dotnet new sln` + `dotnet new webapi` for `YoutubeToLinkedIn.Api`
- Configure CORS for `localhost:4200`, SignalR hub at `/hubs/workflow`, and Minimal API endpoint `POST /api/workflow/start`
- `WorkflowHub.cs` with `SendProgress` broadcast method
- `WorkflowStartEndpoint.cs` returns `{ sessionId: Guid }` (mocked)

### Frontend
- `ng new youtube-to-linkedin-app` (standalone, Angular 21)
- `SignalRService` connects to the hub and exposes an Observable of events
- `WorkflowService.start()` calls the backend POST endpoint
- `AppComponent` with a basic 3-field form (URL, post type, mode)

## Stack

| Layer | Technology |
|---|---|
| Backend runtime | .NET 10 |
| Frontend framework | Angular 21 (standalone) |
| Realtime | ASP.NET Core SignalR + @microsoft/signalr |
| Backend port | https://localhost:5001 |
| Frontend port | http://localhost:4200 |
