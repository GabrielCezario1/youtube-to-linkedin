## ADDED Requirements

### Requirement: Frontend provides .env.example template
The frontend directory SHALL contain a `.env.example` file that documents the required environment variable `BACKEND_URL` with a sensible local default.

#### Scenario: .env.example is present and committed
- **WHEN** a developer clones the repository
- **THEN** `src/frontend/youtube-to-linkedin-app/.env.example` exists and contains `BACKEND_URL=https://localhost:5001`

### Requirement: .env is not committed
The `.gitignore` SHALL exclude `.env` files so that developer-specific environment values are never tracked.

#### Scenario: .env is excluded from version control
- **WHEN** a developer copies `.env.example` to `.env` and fills in values
- **THEN** git does not stage `.env`
