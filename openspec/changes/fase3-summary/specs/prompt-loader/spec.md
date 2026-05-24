## ADDED Requirements

### Requirement: PromptLoader loads all Prompts/*.md files at startup
The system SHALL scan the `Prompts/` directory (relative to `ContentRootPath`) at application startup and read all `*.md` files into an in-memory dictionary keyed by file name without extension (case-insensitive).

#### Scenario: Prompts directory exists with files
- **WHEN** the application starts and `Prompts/` contains one or more `.md` files
- **THEN** each file is read and stored in memory; subsequent calls to `GetPrompt` return the file content without disk I/O

#### Scenario: Prompts directory missing
- **WHEN** the application starts and the `Prompts/` directory does not exist
- **THEN** startup fails with a `DirectoryNotFoundException` (fail-fast behaviour)

### Requirement: PromptLoader exposes prompts by name via GetPrompt
The system SHALL provide a `GetPrompt(string name)` method that returns the content of the prompt file matching `name` (case-insensitive, without extension). If the key is not found, the method SHALL throw a `KeyNotFoundException` with a descriptive message.

#### Scenario: Known prompt name
- **WHEN** `GetPrompt("summarizer-system")` is called after successful startup
- **THEN** the content of `Prompts/summarizer-system.md` is returned as a string

#### Scenario: Unknown prompt name
- **WHEN** `GetPrompt("nonexistent")` is called
- **THEN** a `KeyNotFoundException` is thrown

### Requirement: PromptLoader is registered as Singleton
`PromptLoader` SHALL be registered as `AddSingleton` in `Program.cs` so that the file-loading cost is paid once and all executors share the same loaded prompt dictionary.

#### Scenario: Single load across requests
- **WHEN** multiple requests invoke `SummaryExecutor` concurrently
- **THEN** `GetPrompt` reads from the in-memory dictionary populated at startup with no repeated file I/O
