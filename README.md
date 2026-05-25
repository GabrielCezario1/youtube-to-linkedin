# Tech Content Agent

> 🇧🇷 [Leia em Português](#tech-content-agent-pt-br)

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

---

# Tech Content Agent (PT-BR)

> 🇺🇸 [Read in English](#tech-content-agent)

Converte um vídeo do YouTube em um post para o LinkedIn usando Azure OpenAI. Cole a URL do vídeo, escolha entre o **modo Automático** (totalmente automatizado) ou o **modo Consultivo** (a IA faz perguntas antes de escrever) e obtenha um rascunho pronto para publicar em segundos.

## Visão Geral da Arquitetura

```
Frontend (Angular 21)  ──HTTP──▶  Backend (.NET 10 / ASP.NET Core)  ──▶  Azure OpenAI
         │                                    │
         └──────── SignalR ◀──────────────────┘
```

- **Backend**: Minimal API + hub SignalR. Orquestra a extração de transcrição (YoutubeExplode), sumarização e geração do post via Azure OpenAI.
- **Frontend**: SPA Angular que recebe o progresso do workflow em tempo real via SignalR.

## Pré-requisitos

| Ferramenta | Versão | Link |
|------------|--------|------|
| .NET SDK | 10.0+ | https://dotnet.microsoft.com/download |
| Node.js | 20+ | https://nodejs.org/ |
| npm | 11+ | (incluído com o Node.js) |
| Angular CLI | 21+ | `npm install -g @angular/cli` |
| Azure OpenAI | — | Deployment ativo (ex: `gpt-4o-mini`) |

## Configuração

### 1. Clone o repositório

```bash
git clone <repo-url>
cd youtube-to-linkedin
```

### 2. Configure o backend — credenciais do Azure OpenAI

A chave de API é armazenada via [.NET User Secrets](https://learn.microsoft.com/pt-br/aspnet/core/security/app-secrets) e **nunca é commitada no repositório**.

```bash
cd src/backend/YoutubeToLinkedIn.Api
dotnet user-secrets set "AzureOpenAI:ApiKey" "<sua-chave-de-api>"
```

Em seguida, abra `src/backend/YoutubeToLinkedIn.Api/appsettings.json` e preencha o endpoint do Azure OpenAI:

```json
"AzureOpenAI": {
  "Endpoint": "https://<seu-recurso>.openai.azure.com/",
  "ModelId": "gpt-4o-mini"
}
```

> A `ApiKey` **não deve** ser colocada no `appsettings.json`. Use o User Secrets conforme mostrado acima.

### 3. Configure o ambiente do frontend

```bash
cd src/frontend/youtube-to-linkedin-app
cp .env.example .env
```

O `.env` padrão aponta para o endereço HTTPS do backend. Se usar o perfil HTTP, ajuste conforme necessário:

```env
# Perfil HTTPS (padrão)
BACKEND_URL=https://localhost:7064

# Perfil HTTP (alternativo)
# BACKEND_URL=http://localhost:5224
```

## Rodando Localmente

Abra **dois terminais**.

### Terminal 1 — Backend

```bash
cd src/backend/YoutubeToLinkedIn.Api
dotnet run
```

| Perfil | URL |
|--------|-----|
| HTTPS (padrão) | https://localhost:7064 |
| HTTP | http://localhost:5224 |

Para forçar um perfil específico:

```bash
dotnet run --launch-profile https   # HTTPS
dotnet run --launch-profile http    # somente HTTP
```

### Terminal 2 — Frontend

```bash
cd src/frontend/youtube-to-linkedin-app
npm install
npm start
```

Disponível em: **http://localhost:4200**

## Resumo do Ambiente

| Serviço | URL |
|---------|-----|
| Frontend | http://localhost:4200 |
| Backend (HTTPS) | https://localhost:7064 |
| Backend (HTTP) | http://localhost:5224 |
| Hub SignalR | /hubs/workflow |

## Endpoints Principais

| Método | Caminho | Descrição |
|--------|---------|-----------|
| `POST` | `/api/workflow/start` | Inicia um novo workflow (Automático ou Consultivo) |
| `POST` | `/api/workflow/{sessionId}/respond` | Responde uma pergunta do modo Consultivo |
| `DELETE` | `/api/workflow/{sessionId}` | Cancela um workflow ativo |
| WS | `/hubs/workflow` | Hub SignalR para eventos de progresso em tempo real |

## Modos de Workflow

**Modo Automático** — forneça a URL do YouTube e o agente extrai automaticamente a transcrição, sumariza e gera o post para o LinkedIn.

**Modo Consultivo** — mesmo fluxo, mas o agente faz até 3 perguntas de esclarecimento antes de escrever o post. Sessões inativas expiram após 10 minutos (configurável via `Workflow:ConsultedSessionTimeoutMinutes`).

## Documentação

- [Requisitos do Produto](docs/PRD_TechContentAgent.md)
- [Plano de Implementação](docs/IMPLEMENTATION_PLAN.md)
