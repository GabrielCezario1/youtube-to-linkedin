## Why

The project currently has no documentation or configuration guidance for new developers. A developer cloning the repository cannot run the project without reverse-engineering secrets, environment variables, and startup steps from the source code. This phase closes that gap.

## What Changes

- Add `README.md` at the repository root covering prerequisites, configuration, and local execution steps
- Add `.gitignore` at the repository root covering secrets, build artifacts, IDE files, and OS metadata
- Update `appsettings.json` to declare the full configuration structure with empty/default values and a documented pattern for supplying secrets via User Secrets
- Add `.env.example` in the frontend with `BACKEND_URL` as a reference template
- Ensure `ConsultedSessionTimeoutMinutes` is read from `appsettings.json` (not hardcoded)
- Ensure `AllowedOrigins` is read from `appsettings.json` (not hardcoded in `Program.cs`)

## Capabilities

### New Capabilities

- `project-readme`: Root-level README covering project description, prerequisites (.NET 10, Node 20+, Angular CLI, Azure OpenAI account), configuration steps (User Secrets for ApiKey), and local run commands for backend and frontend
- `gitignore-setup`: Root `.gitignore` that excludes secrets (`appsettings.Development.json`, `.env`, `*.user`), build artifacts (`bin/`, `obj/`, `dist/`, `.angular/`), dependencies (`node_modules/`), IDE folders (`.vs/`, `.idea/`), and OS metadata (`.DS_Store`, `Thumbs.db`)
- `appsettings-structure`: Complete `appsettings.json` with documented empty keys for `AzureOpenAI` (Endpoint, ApiKey, ModelId) and explicit entries for `Workflow.ConsultedSessionTimeoutMinutes` and `AllowedOrigins`
- `frontend-env-example`: `.env.example` file in the frontend with `BACKEND_URL=https://localhost:5001`

### Modified Capabilities

## Impact

- `src/backend/YoutubeToLinkedIn.Api/appsettings.json` — updated to include full configuration structure
- `src/backend/YoutubeToLinkedIn.Api/Program.cs` — CORS origins and session timeout must be read from configuration rather than hardcoded
- `src/frontend/youtube-to-linkedin-app/.env.example` — new file (`.env` remains gitignored)
- `README.md` — new file at repository root
- `.gitignore` — new file at repository root
