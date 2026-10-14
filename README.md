# AnkiHelper

AnkiHelper is a language-learning web app that turns already learned Anki cards into active training exercises.

The app imports Anki `.apkg` packages, lets the user configure which card fields represent the target language and native language, and then generates several training modes for active recall and sentence practice.

## Features

- User registration and login with JWT access tokens and HttpOnly refresh-token cookies.
- Anki `.apkg` import with deck selection and field configuration.
- Local AnkiConnect mode for working directly with a locally running Anki desktop app.
- Deck synchronization by uploading a fresh `.apkg` file.
- Per-deck settings: display name, daily new-note limit, learned-only mode.
- Training modes:
  - Word Order
  - Sentence Composition
  - Translation Sentence
- Training progress and yearly activity statistics.
- Multi-language UI: English, Polish, Ukrainian.
- Responsive UI for desktop and mobile.
- Backend tests for application services and PostgreSQL repositories with Testcontainers.

## Tech Stack

- Backend: ASP.NET Core, EF Core, PostgreSQL, JWT authentication.
- Frontend: React, Vite, React Router.
- AI integration: OpenAI API for language detection and sentence feedback.
- Tests: xUnit, FluentAssertions, NSubstitute, Testcontainers.

## Project Structure

```text
AnkiHelper/              ASP.NET Core API and endpoints
Application/             Use cases, service abstractions, application services
Domain/                  Domain models
Infrastructure/          EF Core, repositories, Anki/APKG adapters, OpenAI integration
Application.Tests/       Unit tests for application logic
Infrastructure.Tests/    PostgreSQL integration tests
ankihelperclient/        React client
```

## Requirements

- .NET 10 SDK
- Node.js
- PostgreSQL
- Docker, required for infrastructure tests
- OpenAI API key

## Configuration

The backend expects these values from user secrets, environment variables, or configuration:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=ankihelper;Username=postgres;Password=your-password"
  },
  "Jwt": {
    "Key": "your-long-secret-key"
  },
  "OpenAI": {
    "ApiKey": "your-openai-api-key"
  }
}
```

Example with user secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=ankihelper;Username=postgres;Password=your-password" --project AnkiHelper
dotnet user-secrets set "Jwt:Key" "your-long-secret-key" --project AnkiHelper
dotnet user-secrets set "OpenAI:ApiKey" "your-openai-api-key" --project AnkiHelper
```

## Run Locally

Apply the database migration:

```powershell
dotnet ef database update --project Infrastructure --startup-project AnkiHelper
```

Run the backend:

```powershell
dotnet run --project AnkiHelper --launch-profile https
```

Run the frontend:

```powershell
cd ankihelperclient
npm install
npm run dev
```

Or start both backend and frontend with the launcher:

```powershell
dotnet run --project AnkiHelper.Launcher
```

To create a Windows launcher executable:

```powershell
dotnet publish AnkiHelper.Launcher -c Release
```

The launcher files will be created in:

```text
AnkiHelper.Launcher/bin/Release/net10.0/publish/
```

Run `AnkiHelper.Launcher.exe` from that folder. Keep the generated `.dll`, `.json`, and `.exe` files together.

The launcher opens the frontend at `http://localhost:56573` to avoid local browser certificate warnings. The backend API still runs over HTTPS.

Default local URLs:

- Frontend: `http://localhost:56573` when started by the launcher, or `https://localhost:56573` when started manually with Vite HTTPS.
- Backend: `https://localhost:7041`

## Tests

```powershell
dotnet test AnkiHelper.slnx --no-restore
```

The infrastructure tests use Testcontainers, so Docker must be running.

## Notes

The default data source is `.apkg` import:

```json
"AnkiDataSource": "Apkg"
```

The app can also run in local AnkiConnect mode:

```json
"AnkiDataSource": "AnkiConnect"
```

In this mode the backend reads decks and cards directly from a locally running Anki desktop app through the AnkiConnect add-on. This is useful for local development and personal use. The `.apkg` flow is the main portable mode for regular users.
