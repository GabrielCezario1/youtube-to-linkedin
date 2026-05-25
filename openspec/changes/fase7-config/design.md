## Context

The project has reached feature-complete status after Phases 1–6. The backend (`YoutubeToLinkedIn.Api`) and frontend (`youtube-to-linkedin-app`) are fully functional but have no developer-facing documentation and two configuration values hardcoded in source:

- `Program.cs` hardcodes `http://localhost:4200` as the CORS allowed origin.
- `WorkflowSessionManager.cs` hardcodes `TimeSpan.FromMinutes(10)` as the session timeout.
- `appsettings.json` lacks the `ApiKey`, `Workflow`, and `AllowedOrigins` keys.
- No `.gitignore` at the repository root.
- No `README.md` at the repository root.
- No `.env.example` in the frontend.

## Goals / Non-Goals

**Goals:**
- Make the project runnable from a fresh clone by following README instructions alone
- Move hardcoded configuration values (`AllowedOrigins`, `ConsultedSessionTimeoutMinutes`) to `appsettings.json`
- Ensure no secrets are committed (ApiKey kept out via User Secrets pattern)
- Provide `.env.example` as a frontend environment template
- Cover all standard gitignore exclusions (secrets, build artifacts, IDE, OS)

**Non-Goals:**
- Production deployment (Docker, Azure, CI/CD)
- API documentation (Swagger/OpenAPI)
- Automated test documentation
- Changelog or semantic versioning

## Decisions

### D1 — User Secrets for ApiKey in development

`AzureOpenAI:ApiKey` is set via `dotnet user-secrets` rather than `appsettings.Development.json`.

**Alternatives considered:**
- `appsettings.Development.json` — simpler but the file is often accidentally committed despite `.gitignore` entries; User Secrets is the idiomatic .NET pattern and is stored outside the repository tree.
- Environment variables — correct approach for production but adds friction in a local dev loop.

**Decision:** Use User Secrets in development; document the `dotnet user-secrets set` command in README. Production environments supply the value via environment variables (standard ASP.NET Core override chain).

### D2 — AllowedOrigins as an array in appsettings.json

CORS origins are read from `IConfiguration` (`AllowedOrigins` string array) instead of being hardcoded in `Program.cs`. `builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()` replaces the literal `.WithOrigins("http://localhost:4200")`.

**Rationale:** Allows different origins per environment without recompiling.

### D3 — ConsultedSessionTimeoutMinutes in appsettings.json

`WorkflowSessionManager` reads the timeout from `IConfiguration["Workflow:ConsultedSessionTimeoutMinutes"]` with a fallback of `10`. The static `SessionTimeout` field is replaced by a constructor-injected value.

**Rationale:** Enables tuning timeout without a code change; consistent with R4 in the PRD.

### D4 — .env.example (not .env) in frontend

Only `.env.example` is committed; `.env` is gitignored. Angular reads `BACKEND_URL` from the environment at build/serve time (or via `environment.ts` proxy config).

**Rationale:** Industry-standard pattern; prevents accidental secret commits.

## Risks / Trade-offs

- **Risk:** Developer forgets to run `dotnet user-secrets set` and gets a runtime exception. → **Mitigation:** `Program.cs` already throws `InvalidOperationException` with a clear message when `ApiKey` is missing; README documents the exact command.
- **Risk:** `AllowedOrigins` array being null/empty breaks CORS silently. → **Mitigation:** Validate the array in `Program.cs`; fall back to an empty-origins policy that will simply reject cross-origin requests with a clear error in the browser console.
- **Risk:** Session timeout of `0` or negative values from misconfiguration. → **Mitigation:** `WorkflowSessionManager` uses `Math.Max(1, value)` to clamp the minimum to 1 minute.
