# Tech Content Agent

Converts a YouTube video into a LinkedIn post using Azure OpenAI. Paste a video URL, choose between **Auto mode** (fully automated) or **Consulted mode** (AI asks clarifying questions before writing), and get a ready-to-publish draft in seconds.

## Architecture Overview

```
Frontend (Angular 21)  ──HTTP──▶  Backend (.NET 10 / ASP.NET Core)  ──▶  Azure OpenAI
         │                                    │
         └──────── SignalR ◀──────────────────┘
```

- **Backend**: Minimal API + SignalR hub. Orchestrates transcript extraction (YoutubeExplode), summarization, and LinkedIn post generation via Azure OpenAI.
- **Frontend**: Angular SPA that streams real-time workflow progress via SignalR.

## Prerequisites

| Tool | Version | Link |
|------|---------|------|
| .NET SDK | 10.0+ | https://dotnet.microsoft.com/download |
| Node.js | 20+ | https://nodejs.org/ |
| npm | 11+ | (bundled with Node.js) |
| Angular CLI | 21+ | `npm install -g @angular/cli` |
| Azure OpenAI | — | Active deployment (e.g. `gpt-4o-mini`) |

## Setup

### 1. Clone the repository

```bash
git clone <repo-url>
cd youtube-to-linkedin
```

### 2. Configure the backend — Azure OpenAI credentials

The API key is stored using [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) and **never committed to source control**.

```bash
cd src/backend/YoutubeToLinkedIn.Api
dotnet user-secrets set "AzureOpenAI:ApiKey" "<your-api-key>"
```

Then open `src/backend/YoutubeToLinkedIn.Api/appsettings.json` and fill in your Azure OpenAI endpoint:

```json
"AzureOpenAI": {
  "Endpoint": "https://<your-resource>.openai.azure.com/",
  "ModelId": "gpt-4o-mini"
}
```

> The `ApiKey` must **not** be placed in `appsettings.json`. Use User Secrets as shown above.

### 3. Configure the frontend environment

```bash
cd src/frontend/youtube-to-linkedin-app
cp .env.example .env
```

The default `.env` points to the backend HTTPS address. If you use the HTTP profile, update accordingly:

```env
# HTTPS profile (default)
BACKEND_URL=https://localhost:7064

# HTTP profile (alternative)
# BACKEND_URL=http://localhost:5224
```

## Running Locally

Open **two terminals**.

### Terminal 1 — Backend

```bash
cd src/backend/YoutubeToLinkedIn.Api
dotnet run
```

| Profile | URL |
|---------|-----|
| HTTPS (default) | https://localhost:7064 |
| HTTP | http://localhost:5224 |

To force a specific profile:

```bash
dotnet run --launch-profile https   # HTTPS
dotnet run --launch-profile http    # HTTP only
```

### Terminal 2 — Frontend

```bash
cd src/frontend/youtube-to-linkedin-app
npm install
npm start
```

Available at: **http://localhost:4200**

## Environment Summary

| Service | URL |
|---------|-----|
| Frontend | http://localhost:4200 |
| Backend (HTTPS) | https://localhost:7064 |
| Backend (HTTP) | http://localhost:5224 |
| SignalR Hub | /hubs/workflow |

## Key Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/api/workflow/start` | Start a new workflow (Auto or Consulted) |
| `POST` | `/api/workflow/{sessionId}/respond` | Answer a clarifying question (Consulted mode) |
| `DELETE` | `/api/workflow/{sessionId}` | Cancel an active workflow |
| WS | `/hubs/workflow` | SignalR hub for real-time progress events |

## Workflow Modes

**Auto Mode** — provide a YouTube URL and the agent automatically extracts the transcript, summarizes it, and generates a LinkedIn post.

**Consulted Mode** — same flow, but the agent pauses to ask up to 3 clarifying questions before writing the post. Idle sessions expire after 10 minutes (configurable via `Workflow:ConsultedSessionTimeoutMinutes`).

## Documentation

- [Product Requirements](docs/PRD_TechContentAgent.md)
- [Implementation Plan](docs/IMPLEMENTATION_PLAN.md)
