## 1. Configuration — Backend

- [x] 1.1 Add `ApiKey` (empty string), `Workflow.ConsultedSessionTimeoutMinutes` (10), and `AllowedOrigins` array to `appsettings.json`
- [x] 1.2 Replace hardcoded `WithOrigins("http://localhost:4200")` in `Program.cs` with `builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()`
- [x] 1.3 Inject `IConfiguration` into `WorkflowSessionManager` and replace the static `SessionTimeout` field with a value read from `Workflow:ConsultedSessionTimeoutMinutes` (minimum 1 minute)

## 2. Configuration — Frontend

- [x] 2.1 Create `src/frontend/youtube-to-linkedin-app/.env.example` with `BACKEND_URL=https://localhost:5001`

## 3. Repository Hygiene

- [x] 3.1 Create `.gitignore` at the repository root covering: `appsettings.Development.json`, `.env`, `*.user`, `bin/`, `obj/`, `node_modules/`, `dist/`, `.angular/`, `.vs/`, `.idea/`, `*.suo`, `.DS_Store`, `Thumbs.db`

## 4. README

- [x] 4.1 Create `README.md` at the repository root with: project description (2–3 lines), prerequisites (.NET 10 SDK, Node.js 20+, Angular CLI, Azure OpenAI account), configuration steps (clone, `dotnet user-secrets init`, `dotnet user-secrets set "AzureOpenAI:ApiKey" "<key>"`, set Endpoint in `appsettings.json`, copy `.env.example` → `.env`), local run commands for backend (`dotnet run` → `https://localhost:5001`) and frontend (`npm install` + `ng serve` → `http://localhost:4200`), and links to `docs/PRD_TechContentAgent.md` and `docs/IMPLEMENTATION_PLAN.md`

## 5. Verification

- [x] 5.1 Verify `appsettings.json` has no real secrets committed (ApiKey is empty string)
- [x] 5.2 Verify `.gitignore` excludes `appsettings.Development.json` and `.env` by running `git status` after creating those files
- [x] 5.3 Verify backend starts and reads AllowedOrigins from config (no hardcoded origin in `Program.cs`)
- [x] 5.4 Verify backend starts and applies the session timeout from config (no hardcoded constant in `WorkflowSessionManager.cs`)
