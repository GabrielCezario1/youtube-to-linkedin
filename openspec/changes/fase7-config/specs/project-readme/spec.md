## ADDED Requirements

### Requirement: README documents prerequisites
The repository root SHALL contain a `README.md` that lists all prerequisites required to run the project locally, including minimum version numbers.

#### Scenario: Prerequisites are listed with versions
- **WHEN** a developer opens `README.md`
- **THEN** the file lists .NET 10 SDK, Node.js 20+, Angular CLI, and an Azure OpenAI account with an active deployment

### Requirement: README documents configuration steps
The README SHALL provide step-by-step instructions for configuring the project before first run, including the exact `dotnet user-secrets` commands for supplying the API key.

#### Scenario: Developer follows configuration steps
- **WHEN** a developer follows the Configuration section in README
- **THEN** they can supply `AzureOpenAI:ApiKey` via `dotnet user-secrets set` without editing any committed file

#### Scenario: README includes Endpoint and ModelId instructions
- **WHEN** a developer reads the configuration section
- **THEN** the README instructs them to set `AzureOpenAI:Endpoint` and `AzureOpenAI:ModelId` in `appsettings.json`

### Requirement: README documents local run steps
The README SHALL provide commands to start the backend and frontend, with the expected URL for each.

#### Scenario: Backend startup documented
- **WHEN** a developer follows the backend run instructions
- **THEN** they run `dotnet run` from `src/backend/YoutubeToLinkedIn.Api` and the README states the service is available at `https://localhost:5001`

#### Scenario: Frontend startup documented
- **WHEN** a developer follows the frontend run instructions
- **THEN** they run `npm install` then `ng serve` from `src/frontend/youtube-to-linkedin-app` and the README states the app is available at `http://localhost:4200`

### Requirement: README links to documentation
The README SHALL include references to `docs/PRD_TechContentAgent.md` and `docs/IMPLEMENTATION_PLAN.md` for further reading.

#### Scenario: Documentation links are present
- **WHEN** a developer reads the README
- **THEN** hyperlinks to the PRD and implementation plan are present and point to the correct relative paths
