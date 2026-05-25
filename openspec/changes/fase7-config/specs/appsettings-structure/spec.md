## ADDED Requirements

### Requirement: appsettings.json declares full AzureOpenAI structure
`appsettings.json` SHALL declare `AzureOpenAI.Endpoint`, `AzureOpenAI.ApiKey` (empty string), and `AzureOpenAI.ModelId` (defaulting to `"gpt-4o-mini"`).

#### Scenario: ApiKey is empty in committed file
- **WHEN** a developer inspects `appsettings.json`
- **THEN** `AzureOpenAI.ApiKey` is an empty string and the file contains no real credentials

#### Scenario: ModelId has a default value
- **WHEN** the backend starts without overriding ModelId
- **THEN** the default value `"gpt-4o-mini"` is used

### Requirement: appsettings.json declares Workflow configuration
`appsettings.json` SHALL declare `Workflow.ConsultedSessionTimeoutMinutes` with a default value of `10`.

#### Scenario: Session timeout is configurable
- **WHEN** an operator changes `Workflow:ConsultedSessionTimeoutMinutes` in `appsettings.json`
- **THEN** the backend uses the new timeout value without recompilation

### Requirement: appsettings.json declares AllowedOrigins
`appsettings.json` SHALL declare `AllowedOrigins` as a JSON array containing `"http://localhost:4200"` as the default.

#### Scenario: CORS origin is configurable
- **WHEN** an operator changes `AllowedOrigins` in `appsettings.json`
- **THEN** the backend applies the new CORS policy without recompilation

### Requirement: Program.cs reads AllowedOrigins from configuration
`Program.cs` SHALL read CORS origins from `IConfiguration` using the `AllowedOrigins` key instead of a hardcoded string.

#### Scenario: CORS configured from appsettings
- **WHEN** the backend starts
- **THEN** the CORS policy is built from the `AllowedOrigins` array in configuration

### Requirement: WorkflowSessionManager reads timeout from configuration
`WorkflowSessionManager` SHALL read the session timeout from `IConfiguration["Workflow:ConsultedSessionTimeoutMinutes"]` and SHALL enforce a minimum of 1 minute.

#### Scenario: Timeout read from config
- **WHEN** `WorkflowSessionManager` initialises
- **THEN** it uses the value from `appsettings.json` rather than a hardcoded constant

#### Scenario: Minimum timeout enforced
- **WHEN** `Workflow:ConsultedSessionTimeoutMinutes` is set to `0` or a negative number
- **THEN** the session manager uses `1` minute as the effective timeout
