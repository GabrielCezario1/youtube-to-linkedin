## ADDED Requirements

### Requirement: Root .gitignore excludes secrets
The repository root SHALL contain a `.gitignore` that prevents committing `appsettings.Development.json`, `.env`, and `*.user` files.

#### Scenario: Secrets are excluded
- **WHEN** a developer creates `appsettings.Development.json` or `.env`
- **THEN** git does not stage those files

### Requirement: Root .gitignore excludes backend build artifacts
The `.gitignore` SHALL exclude `bin/` and `obj/` directories produced by the .NET build.

#### Scenario: Build artifacts are excluded
- **WHEN** the backend is built
- **THEN** `bin/` and `obj/` directories are not tracked by git

### Requirement: Root .gitignore excludes frontend build artifacts
The `.gitignore` SHALL exclude `node_modules/`, `dist/`, and `.angular/` directories produced by the Angular toolchain.

#### Scenario: Frontend artifacts are excluded
- **WHEN** `npm install` or `ng build` is run
- **THEN** `node_modules/`, `dist/`, and `.angular/` are not tracked by git

### Requirement: Root .gitignore excludes IDE and OS files
The `.gitignore` SHALL exclude `.vs/`, `.idea/`, `*.suo`, `.DS_Store`, and `Thumbs.db`.

#### Scenario: IDE and OS metadata are excluded
- **WHEN** a developer opens the project in Visual Studio, VS Code, or on macOS
- **THEN** IDE-generated and OS-generated files are not tracked by git
