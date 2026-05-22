# CineMatch — Backend

[![Build and Test](https://github.com/Burra17/CineMatch-Backend/actions/workflows/build-and-test.yml/badge.svg?branch=main)](https://github.com/Burra17/CineMatch-Backend/actions/workflows/build-and-test.yml)

CineMatch är en webb-applikation där flera användare kan gå med i ett "WatchParty" och swipea på filmer tillsammans (Tinder-style). När alla aktiva medlemmar i ett party har gillat samma film skapas en match och visas för alla. Syftet är att hjälpa vänner eller par att hitta en film de alla vill se — utan ändlösa diskussioner.

Det här repot innehåller **backend-API:t**. Frontenden (React) finns i ett separat repo.

## Teknikstack

| Kategori | Teknik |
|---|---|
| Ramverk | .NET 10 Web API |
| ORM / Databas | Entity Framework Core + PostgreSQL (Npgsql) |
| CQRS | MediatR |
| Resultat-mönster | ErrorOr |
| Validering | FluentValidation |
| Mapping | AutoMapper |
| Autentisering | JWT (HS256) |
| Lösenordshashning | BCrypt |
| API-dokumentation | Scalar |
| Extern datakälla | TMDB API (filmdata) |
| Tester | NUnit + NSubstitute |

## Förutsättningar

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [PostgreSQL](https://www.postgresql.org/download/) (lokal instans eller Docker)
- Ett gratis [TMDB API-konto](https://www.themoviedb.org/settings/api) för filmdata

## Kom igång lokalt

### 1. Klona repot

```bash
git clone https://github.com/Burra17/CineMatch-Backend.git
cd CineMatch-Backend
```

### 2. Konfigurera databasen

Skapa en PostgreSQL-databas och uppdatera `DefaultConnection` i `src/CineMatch.API/appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Database=cinematch;Username=postgres;Password=yourpassword"
}
```

### 3. Sätt hemligheter via user secrets

Hemligheter lagras aldrig i källkoden. Kör följande från `src/CineMatch.API`-mappen:

```bash
dotnet user-secrets set "JwtSettings:Secret" "<minst-32-tecken-lång-hemlighet>"
dotnet user-secrets set "Tmdb:ApiKey" "<din-tmdb-api-nyckel>"
```

Vill du att en admin-användare skapas automatiskt vid uppstart sätter du även:

```bash
dotnet user-secrets set "AdminSeed:Email" "admin@cinematch.com"
dotnet user-secrets set "AdminSeed:Username" "admin"
dotnet user-secrets set "AdminSeed:Password" "<ett-säkert-lösenord>"
```

Om dessa är tomma (standardvärdet i `appsettings.json`) hoppar applikationen över seeding utan fel. Admin skapas bara om ingen användare med rollen `Admin` redan finns — körning flera gånger skapar inga dubbletter.

### 4. Kör migrationer

```bash
dotnet ef database update \
  --project src/CineMatch.Infrastructure \
  --startup-project src/CineMatch.API
```

### 5. Starta API:t

```bash
dotnet run --project src/CineMatch.API
```

API:t startar på `https://localhost:7179` (eller den port som konfigureras i `launchSettings.json`).

## API-dokumentation

Scalar-dokumentationen är tillgänglig på **`/scalar`** när projektet körs:

```
https://localhost:7179/scalar
```

Alla endpoints, parametrar och exempel-responses finns dokumenterade där.

## Kör tester

```bash
dotnet test
```

Alla tester körs mot mockade repositories och externa tjänster — ingen riktig databas eller TMDB-anrop krävs.

## CI

Build och tester körs automatiskt av GitHub Actions via `.github/workflows/build-and-test.yml` vid varje pull request och push till `main`. Branch protection på `main` kräver att CI är grön innan merge.

## Dokumentation

- [UML Class Diagram](docs/uml-class-diagram.md)
- [User Flow Diagram](docs/userFlow-diagram.md)
- [Kodstandard](docs/coding-standards.md)
