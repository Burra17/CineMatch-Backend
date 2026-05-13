# CineMatch

[![Build and Test](https://github.com/Burra17/CineMatch-Backend/actions/workflows/build-and-test.yml/badge.svg?branch=main)](https://github.com/Burra17/CineMatch-Backend/actions/workflows/build-and-test.yml)

scalar för api documentation https://localhost:7179/scalar/

[Klicka här för att se vårt UML Class Diagram](docs/uml-class-diagram.md)

## Local Setup

### User Secrets

The application requires secrets that are not stored in source control. Set them via the .NET user-secrets tool from the `src/CineMatch.API` directory:

```bash
dotnet user-secrets set "JwtSettings:Secret" "<your-jwt-secret-min-32-chars>"
dotnet user-secrets set "Tmdb:ApiKey" "<your-tmdb-api-key>"
```

You can obtain a free TMDB API key at [themoviedb.org](https://www.themoviedb.org/settings/api).

## Build & Test (CI)

Build och tester körs automatiskt av GitHub Actions via `.github/workflows/build-and-test.yml`.

**När det körs:**
- Vid varje pull request mot `main`
- Vid push till `main` (verifierar efter merge)

**Vad jobbet gör:**
1. Sätter upp .NET 10 SDK på `ubuntu-latest`
2. Cachear NuGet-paket baserat på hash av alla `.csproj`
3. `dotnet restore CineMatch.slnx`
4. `dotnet build CineMatch.slnx --configuration Release --no-restore`
5. `dotnet test CineMatch.slnx --configuration Release --no-build`

Branch protection på `main` kräver att workflow-statusen är grön innan PR:n får mergas. Badge ovan visar status på senaste körningen mot `main`.