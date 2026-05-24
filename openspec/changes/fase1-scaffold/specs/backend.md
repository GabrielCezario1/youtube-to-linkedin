# Spec: Backend — API e SignalR Hub

## Overview

The backend is an ASP.NET Core Minimal API (.NET 10) with SignalR. It exposes one HTTP endpoint and one SignalR hub. All data in this phase is mocked.

## Endpoint: POST /api/workflow/start

**Purpose**: Accept a workflow start request and return a unique session identifier.

**Request**
```json
{
  "url": "string",
  "postType": "string",
  "mode": "string"
}
```

**Response** (200 OK)
```json
{
  "sessionId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
}
```

**Behavior**:
1. Generate a new `Guid` as `sessionId`
2. Broadcast a mock `SendProgress` event via the SignalR hub with the `sessionId`
3. Return `{ sessionId }`

## SignalR Hub: /hubs/workflow

**Class**: `WorkflowHub : Hub`

**Server → Client method**: `SendProgress(string sessionId, string message)`

The hub itself is stateless transport. Clients connect and listen for `SendProgress` events. The endpoint triggers the broadcast after generating the sessionId.

## Program.cs Configuration

```csharp
// CORS: allow localhost:4200 in Development only
builder.Services.AddCors(options => {
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// SignalR
builder.Services.AddSignalR();

// Map hub
app.MapHub<WorkflowHub>("/hubs/workflow");

// Map endpoint
app.MapPost("/api/workflow/start", WorkflowStartEndpoint.Handle);
```

## NuGet Dependencies

All packages are installed in this phase even if not fully used until later:

```
dotnet add package Microsoft.Agents.AI --prerelease
dotnet add package Microsoft.Agents.AI.Workflows --prerelease
dotnet add package YoutubeExplode --version 6.6.0
dotnet add package Azure.AI.OpenAI
```
