# Tech Content Agent

Converts a YouTube video into a LinkedIn post using Azure OpenAI. Paste a video URL, choose between Auto mode (fully automated) or Consulted mode (AI asks clarifying questions before writing), and get a ready-to-publish draft in seconds.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/) and npm
- Angular CLI: `npm install -g @angular/cli`
- Azure OpenAI account with an active model deployment (e.g., `gpt-4o-mini`)

## Configuration

### 1. Clone the repository

```bash
git clone <repo-url>
cd youtube-to-linkedin
```

### 2. Set the Azure OpenAI API key (User Secrets)

```bash
cd src/backend/YoutubeToLinkedIn.Api
dotnet user-secrets init
dotnet user-secrets set "AzureOpenAI:ApiKey" "<your-api-key>"
```

### 3. Set the Azure OpenAI endpoint and model

Open `src/backend/YoutubeToLinkedIn.Api/appsettings.json` and fill in:

```json
"AzureOpenAI": {
  "Endpoint": "https://<your-resource>.openai.azure.com/",
  "ApiKey": "",        // leave empty — use User Secrets above
  "ModelId": "gpt-4o-mini"
}
```

### 4. Configure the frontend environment

```bash
cd src/frontend/youtube-to-linkedin-app
cp .env.example .env
```

Edit `.env` if your backend runs on a different port or host.

## Running Locally

### Backend

```bash
cd src/backend/YoutubeToLinkedIn.Api
dotnet run
```

Available at: `https://localhost:5001`

### Frontend

```bash
cd src/frontend/youtube-to-linkedin-app
npm install
ng serve
```

Available at: `http://localhost:4200`

## Documentation

- [Product Requirements](docs/PRD_TechContentAgent.md)
- [Implementation Plan](docs/IMPLEMENTATION_PLAN.md)
