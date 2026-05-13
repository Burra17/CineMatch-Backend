# CLAUDE.md

This document provides Claude (and other AI assistants) with context about the CineMatch backend project. Read this before suggesting code, making changes, or creating new files.

## Project Overview

CineMatch is a web application where multiple users can join a "WatchParty" and swipe on movies together (Tinder-style). When all members of the party have liked the same movie, a match is created and shown to everyone. The goal is to help friends or couples find a movie everyone wants to watch without endless discussion.

This repository contains only the backend API. The frontend (React) lives in a separate repository.

## Tech Stack

- **.NET 10** (Web API)
- **Entity Framework Core** with PostgreSQL (Npgsql)
- **MediatR** for CQRS
- **ErrorOr** for the Result pattern and expected application errors
- **FluentValidation** for input validation
- **AutoMapper** for entity-to-DTO mapping
- **JWT** for authentication
- **BCrypt** for password hashing
- **Scalar** for API documentation
- **NUnit** for testing with **NSubstitute** for mocking
- **TMDB API** as external data source for movies (cached in our database)

## Architecture

We follow Clean Architecture with four projects. Dependencies always point inward — outer layers depend on inner layers, never the other way around.

```
CineMatch.API            → CineMatch.Application, CineMatch.Infrastructure
CineMatch.Infrastructure → CineMatch.Application, CineMatch.Domain
CineMatch.Application    → CineMatch.Domain
CineMatch.Domain         → (no dependencies)
```

### Solution Layout

The four production projects live under `src/`. The test project sits at the repo root.

```
CineMatch-Backend/
  src/
    CineMatch.API/
    CineMatch.Application/
    CineMatch.Infrastructure/
    CineMatch.Domain/
  CineMatch.Tests/
```

### CineMatch.Domain
- Entities, value objects, enums, domain logic
- No external dependencies (no NuGet packages beyond standard library)
- Pure C# — no EF Core, no MediatR, nothing
- Entities live under `Models/`, enums under `Enums/`

### CineMatch.Application
- CQRS commands and queries via MediatR
- Handlers, DTOs, interfaces (for repositories and external services)
- FluentValidation validators
- AutoMapper profiles
- Pipeline behaviours (validation, logging)
- Feature-scoped DTOs and error classes (under `Features/{Entity}/Common/`)

### CineMatch.Infrastructure
- EF Core `AppDbContext` and entity configurations (under `Database/`)
- Repository implementations (interfaces defined in Application)
- TMDB client (implements `ITmdbService` from Application)
- JWT token generation, BCrypt password hashing
- Migrations
- Strongly-typed settings (e.g., `JwtSettings`) under `Database/Configurations/`

### CineMatch.API
- Controllers (thin — just dispatch commands/queries via MediatR)
- Middleware (exception handling, authentication)
- `Common/ResultExtensions.cs` for mapping `ErrorOr<T>` to `IActionResult`
- `Contracts/` for API-level response shapes (e.g., `ErrorResponse`)
- Program.cs and DI configuration
- appsettings

### Tests
- `CineMatch.Tests` — separate project focused on Application handlers

## Domain Model

### Entities

**User**
- Id (Guid), Username (string), Email (string), PasswordHash (string), Role (UserRole), IsEmailConfirmed (bool), CreatedAt (DateTime), UpdatedAt (DateTime?)

**WatchParty**
- Id (Guid), JoinCode (string), HostId (Guid), Genre (string), IsActive (bool), CreatedAt (DateTime), ClosedAt (DateTime?)

**PartyMember** (join table between User and WatchParty)
- Id (Guid), UserId (Guid), WatchPartyId (Guid), JoinedAt (DateTime), LeftAt (DateTime?), IsActive (bool)

**Movie** (cached from TMDB)
- Id (Guid), TmdbId (int, unique), Title (string), PosterUrl (string), Overview (string), ReleaseYear (int), CachedAt (DateTime)

**Swipe**
- Id (Guid), PartyMemberId (Guid), WatchPartyId (Guid), MovieId (Guid), IsLiked (bool), SwipedAt (DateTime)
- Unique constraint: `(PartyMemberId, MovieId)` — a user can only swipe once per movie in the same party

**WatchPartyMovie** (join table between WatchParty and Movie)
- Id (Guid), WatchPartyId (Guid), MovieId (Guid), OrderIndex (int), AddedAt (DateTime)
- Unique constraints: `(WatchPartyId, MovieId)` and `(WatchPartyId, OrderIndex)`

**Match**
- Id (Guid), WatchPartyId (Guid), MovieId (Guid), MatchedAt (DateTime), IsWatched (bool), WatchedByUserId (Guid?), WatchedAt (DateTime?)

**PasswordResetToken**
- Id (Guid), UserId (Guid), TokenHash (string, max 64 chars), ExpiresAt (DateTime), IsUsed (bool), CreatedAt (DateTime), UsedAt (DateTime?)
- `TokenHash` stores the SHA-256 hex hash of the raw token (not BCrypt — must be deterministic for lookup). SHA-256 hex is always exactly 64 characters, hence `HasMaxLength(64)`.
- Unique constraint on `TokenHash`

### Enum

**UserRole**
- `User = 0`
- `Admin = 1`


### Relationships

- User 1 — * WatchParty (host)
- User 1 — * PartyMember and WatchParty 1 — * PartyMember (many-to-many via PartyMember)
- WatchParty 1 — * WatchPartyMovie and Movie 1 — * WatchPartyMovie (many-to-many via WatchPartyMovie)
- WatchParty 1 — * Swipe, PartyMember 1 — * Swipe, Movie 1 — * Swipe
- WatchParty 1 — * Match, Movie 1 — * Match
- User 1 — * PasswordResetToken (cascade delete — orphaned tokens are meaningless without a user)

## Business Logic

### Match Condition
A match is created when **all active members of the party have liked the same movie**. This is evaluated every time a swipe is saved by `IMatchDetectionService.DetectMatchAsync`. If the number of likes on a movie equals the active member count for the party → create a Match. The service is pure (no side effects) — the caller (`CreateSwipeCommandHandler`) is responsible for persisting the match.

### TMDB Caching
Movies are fetched from TMDB **once** when a WatchParty is created (`MoviesPerParty = 50` movies per party, based on the genre). Movies are saved in our `Movies` table using `TmdbId` as the unique identifier. If a movie already exists in the database it is reused. The fetch-and-link step creates `WatchPartyMovie` rows that fix the swipe order for all members. The TMDB API key must be set via user secrets: `dotnet user-secrets set "Tmdb:ApiKey" "<your-api-key>"` (uses v3 API-key auth, not the v4 Read Access Token).

### JoinCode
Generated by `IJoinCodeGenerator` when a WatchParty is created: 6 characters, cryptographic randomness (`RandomNumberGenerator`), and an alphabet that skips visually ambiguous characters (no O/0, I/1, l). Uniqueness is enforced by a **filtered unique index** in PostgreSQL — codes only need to be unique among active parties, so inactive parties' codes can be reused.

### Soft Delete for PartyMember
Leaving a party does **not** delete the row. `PartyMember.IsActive` is set to `false` and `LeftAt` is stamped. Rejoining reactivates the existing row (`IsActive=true`, `LeftAt=null`) instead of inserting a duplicate — there is a composite unique constraint on `(UserId, WatchPartyId)`. This preserves the original `JoinedAt` and the membership history.

### Host Behavior
Only the host can start the swipe session. Host-transfer is not implemented — a host cannot leave the party while other active members remain (returns `WatchPartyErrors.HostCannotLeave`). If the host is the last active member, leaving closes the party (`IsActive=false`, `ClosedAt` set).

### Password Reset Flow
Two-step flow, both endpoints on `AuthController`:

1. `POST /api/Auth/forgot-password` (`RequestPasswordResetCommand`):
   - Looks up the user by email. Returns `200 OK` with the raw token regardless of whether the email exists (no user enumeration).
   - Generates 32 cryptographically random bytes → encodes as Base64 (raw token for the caller).
   - Computes `SHA-256` of those same bytes → stores as uppercase hex string in `PasswordResetToken.TokenHash`.
   - Token expires in 1 hour. In development the raw token is returned in the response body; in production it would be emailed.

2. `POST /api/Auth/reset-password` (`ResetPasswordCommand`):
   - Accepts `Token` (raw Base64) and `NewPassword` (min 8 chars, max 100 chars).
   - Decodes Base64 → recomputes SHA-256 → looks up by hash. A `FormatException` on decode is treated as an invalid token (same error, no leak).
   - Single guard: null OR expired OR `IsUsed` → `PasswordResetErrors.InvalidOrExpiredToken` (no distinction to caller).
   - On success: hashes the new password with BCrypt, stamps `IsUsed=true` and `UsedAt=UtcNow`, returns `204 No Content`.

**Why SHA-256 instead of BCrypt for token hashing:** BCrypt is salted and non-deterministic — you cannot reproduce the same hash from the same input, making lookup impossible. SHA-256 is deterministic and appropriate here because the raw token already has 256 bits of entropy (32 random bytes).

## Application Layer Structure

Use CQRS with one folder per feature. Each command and query has its own folder. Filenames must include the suffix `Command` / `Query` and the handler/validator must repeat that suffix (so it is clear at a glance whether a class belongs to a command or a query).

```
CineMatch.Application/
  Features/
    Users/
      Commands/
        RegisterUser/
          RegisterUserCommand.cs
          RegisterUserCommandHandler.cs
          RegisterUserCommandValidator.cs
        RequestPasswordReset/
          RequestPasswordResetCommand.cs
          RequestPasswordResetCommandHandler.cs
        ResetPassword/
          ResetPasswordCommand.cs
          ResetPasswordCommandHandler.cs
          ResetPasswordCommandValidator.cs
      Queries/
        GetCurrentUser/
          GetCurrentUserQuery.cs
          GetCurrentUserQueryHandler.cs
      Common/
        Dtos/UserDto.cs
        Errors/UserErrors.cs
        Errors/PasswordResetErrors.cs
        Mappings/UserMappingProfile.cs     (per-feature mapping profile)
    WatchParties/
      Commands/
        CreateWatchParty/
          CreateWatchPartyCommand.cs
          CreateWatchPartyCommandHandler.cs
      Queries/
        GetWatchPartyDetails/
          GetWatchPartyDetailsQuery.cs
          GetWatchPartyDetailsQueryHandler.cs
      Common/
        Dtos/
        Errors/
        Mappings/
  Common/
    Behaviours/
      ValidationBehaviour.cs
      LoggingBehaviour.cs
  Interfaces/
    Repositories/
      IGenericRepository.cs
      IUnitOfWork.cs
      IUserRepository.cs
      IWatchPartyRepository.cs
      IPartyMemberRepository.cs
      IMovieRepository.cs
      ISwipeRepository.cs
      IMatchRepository.cs
      IWatchPartyMovieRepository.cs
      IPasswordResetTokenRepository.cs
    Services/
      ICurrentUserService.cs
      IJwtService.cs
      IPasswordHasher.cs
      IJoinCodeGenerator.cs
      ITmdbService.cs
      IMatchDetectionService.cs
  Services/
    MatchDetectionService.cs        (application service — domain logic, no external deps)
  DependencyInjection.cs
```

AutoMapper profiles live next to the feature they map (e.g., `Features/Users/Common/Mappings/UserMappingProfile.cs`), not in a central `Common/Mappings/` folder. They are auto-discovered via `services.AddAutoMapper(cfg => cfg.AddMaps(...))`.

### Naming for Commands, Queries, Handlers, and Validators

| Concern   | Pattern                              | Example                              |
|-----------|--------------------------------------|--------------------------------------|
| Command   | `{Verb}{Entity}Command`              | `RegisterUserCommand`                |
| Query     | `{Verb}{Entity}Query`                | `GetUserByIdQuery`                   |
| Handler   | `{CommandOrQueryName}Handler`        | `RegisterUserCommandHandler`         |
| Validator | `{CommandOrQueryName}Validator`      | `RegisterUserCommandValidator`       |
| DTO       | `{Entity}Dto` (or `{Verb}{Entity}Response` when the shape is action-specific) | `UserDto`         |
| Errors    | `{Entity}Errors` (static class)      | `UserErrors`                         |

Rules:
- Handlers and validators must include the full `Command` or `Query` suffix from the request name — never shorten to `RegisterUserHandler`.
- The folder name matches the request name without the suffix (e.g., folder `RegisterUser/` contains `RegisterUserCommand.cs` + `...CommandHandler.cs` + `...CommandValidator.cs`).
- DTOs are stored per feature under `Features/{Entity}/Common/Dtos/`, not in a global `Dtos/` folder.
- Static error classes live under `Features/{Entity}/Common/Errors/` and expose `Error` properties (see `UserErrors`).

## Code Conventions

### Naming
- Classes, methods, properties: `PascalCase`
- Private fields: `_camelCase`
- Local variables and parameters: `camelCase`
- Interfaces: `IPascalCase`
- Async methods: always suffix with `Async`

### General
- One class per file, filename matches class name
- No magic numbers or strings — use constants or enums
- Comments explain **why**, not **what**
- Aim for 20-30 lines per method maximum
- No `new` instantiation of services — use DI

### CQRS Rules
- Commands change state, Queries read state — never mix
- Handlers should be small and focused — extract logic to domain services if needed
- One command/query per file
- Return DTOs from queries, never entities
- Commands and queries should return `ErrorOr<T>` from MediatR handlers
- Use `ErrorOr` for expected business/application failures instead of throwing custom exceptions

### DTOs
- All client communication uses DTOs
- AutoMapper handles entity ↔ DTO mapping
- DTOs live in the Application layer

### Validation
- FluentValidation for all commands and DTOs that take input
- Validators registered as Pipeline Behavior in MediatR — runs automatically before handler
- Validation errors are converted to `Error.Validation(...)` by `ValidationBehaviour<TRequest, TResponse>`
- MediatR responses that use validation must implement `IErrorOr` (normally by returning `ErrorOr<T>`)

### Repositories
- Generic `IGenericRepository<T>` for standard CRUD — registered as open generic in Infrastructure DI
- Specific repositories for entity-specific logic (e.g., `IWatchPartyRepository.GetByJoinCodeAsync`)
- Repositories return entities, never DTOs
- All database logic lives in repositories — never in handlers
- Use `IUnitOfWork.SaveChangesAsync()` to persist changes — never call `DbContext.SaveChanges` directly in handlers or repositories

### Error Handling
- Use the `ErrorOr` Result pattern for expected errors: validation failures, not found, conflicts, forbidden actions, and invalid business operations
- Do not create custom exceptions for normal domain/application flow such as `WatchPartyNotFoundException` or `InvalidJoinCodeException`
- Define reusable errors as static `Error` properties on a per-entity class (e.g., `UserErrors.EmailAlreadyExists`) under `Features/{Entity}/Common/Errors/`
- Handlers return either a success value or one or more `Error` values:
  ```csharp
  public record GetWatchPartyByIdQuery(Guid Id) : IRequest<ErrorOr<WatchPartyDto>>;

  public class GetWatchPartyByIdQueryHandler
      : IRequestHandler<GetWatchPartyByIdQuery, ErrorOr<WatchPartyDto>>
  {
      public async Task<ErrorOr<WatchPartyDto>> Handle(
          GetWatchPartyByIdQuery request,
          CancellationToken cancellationToken)
      {
          var watchParty = await _watchPartyRepository.GetByIdAsync(request.Id, cancellationToken);

          if (watchParty is null)
          {
              return WatchPartyErrors.NotFound;
          }

          return _mapper.Map<WatchPartyDto>(watchParty);
      }
  }
  ```
- Controllers stay thin: dispatch via MediatR and map `ErrorOr<T>` to `IActionResult` using the shared `ResultExtensions.ToActionResult(this)` extension in `CineMatch.API/Common`
- Map `Error.Validation` to 400, `Error.Unauthorized` to 401, `Error.Forbidden` to 403, `Error.NotFound` to 404, `Error.Conflict` to 409, and unexpected/unmapped errors to 500 (see `ResultExtensions`)
- Centralized exception middleware (`ExceptionHandlingMiddleware`) in the API layer is only for unhandled/unexpected exceptions and returns the `ErrorResponse` contract
- Use try/catch sparingly — let unexpected exceptions bubble up to middleware

### Authentication
- JWT with reasonable lifetime (1 hour)
- Passwords always hashed with BCrypt — never plaintext. Minimum password length is **8 characters** (enforced by `RegisterUserCommandValidator` and `ResetPasswordCommandValidator`).
- `[Authorize]` on protected endpoints
- `[Authorize(Roles = "Admin")]` for role-based access
- JWT secret in user secrets or appsettings — never hardcoded or committed
- Read the logged-in user inside handlers via `ICurrentUserService.UserId` — never via `HttpContext`. The interface lives in `Application/Interfaces/Services/`, the implementation in `API/Services/CurrentUserService.cs`. Returns `null` when no user is authenticated; handlers convert that to `WatchPartyErrors.Unauthorized` / `UserErrors.InvalidCredentials`.
- Refresh tokens are **not implemented** — the 1-hour JWT lifetime is sufficient for the current scope.

### Tests
- NUnit framework, **NSubstitute** for mocking
- Focus on Application handlers (plus targeted Infrastructure services like `JoinCodeGenerator`)
- Naming convention: `MethodName_Scenario_ExpectedResult`, e.g., `Handle_ValidCommand_ReturnsWatchPartyId`
- Tests must be independent and runnable in any order
- Use `[SetUp]` for per-test initialization, `[OneTimeSetUp]` for one-time setup
- AAA pattern (Arrange / Act / Assert) with `Assert.That(...)` (NUnit 4 style)
- Mock fields named `_xxxMock` (e.g., `_userRepositoryMock`)
- Mock repositories and external services — never hit the real DB or TMDB

Example test structure:
```csharp
[TestFixture]
public class CreateWatchPartyHandlerTests
{
    [SetUp]
    public void SetUp()
    {
        // Initialize per test
    }

    [Test]
    public async Task Handle_ValidCommand_ReturnsWatchPartyId()
    {
        // Arrange
        // Act
        // Assert
        Assert.That(result, Is.Not.EqualTo(Guid.Empty));
    }
}
```

## Dependency Injection

- Each project (Application, Infrastructure) has its own `DependencyInjection.cs` with an extension method
- Program.cs only calls the extension methods:
  ```csharp
  builder.Services.AddApplication();
  builder.Services.AddInfrastructure(builder.Configuration);
  ```
- Never register services directly in Program.cs

## Git Conventions

### Branches
- `feature/` — new features
- `bugfix/` — bug fixes
- `refactor/` — refactoring
- `docs/` — documentation
- `test/` — tests

Use kebab-case, descriptive names. No personal names or dates.

### Commits
Imperative form in English:
- ✅ `Add JWT authentication to UserController`
- ✅ `Fix null reference in MatchDetectionService`
- ❌ `fixed stuff`
- ❌ `wip`

Small, focused commits.

### PRs
- All changes go through PR to `main`
- At least one team member must approve
- `main` is protected — direct push not allowed

## Common Tasks

### Add a new entity
1. Create the entity in `src/CineMatch.Domain/Models`
2. Configure EF Core in `src/CineMatch.Infrastructure/Database/Configurations`
3. Add `DbSet` to `AppDbContext`
4. Create migration: `dotnet ef migrations add AddMyEntity --project src/CineMatch.Infrastructure --startup-project src/CineMatch.API`
5. Create repository (interface in Application `Interfaces/Repositories/`, implementation in Infrastructure `Database/Repositories/`) — extend `GenericRepository<T>` when possible
6. Create feature folder `src/CineMatch.Application/Features/MyEntity/Common/{Dtos,Errors,Mappings}/` and add `MyEntityDto`, `MyEntityErrors`, `MyEntityMappingProfile`
7. AutoMapper profiles live per-feature in `Features/MyEntity/Common/Mappings/MyEntityMappingProfile.cs` — they are auto-discovered, no manual registration needed. For positional record DTOs use `.ForCtorParam` rather than `.ForMember`
8. Create CQRS features (commands/queries) in `src/CineMatch.Application/Features/MyEntity/{Commands,Queries}/` using the naming pattern `{Verb}{Entity}Command`, `{Verb}{Entity}CommandHandler`, `{Verb}{Entity}CommandValidator`
9. Create controller in `src/CineMatch.API/Controllers`
10. Register the repository in `src/CineMatch.Infrastructure/DependencyInjection.cs`
11. Write tests for handlers in `CineMatch.Tests`

### Add a new endpoint
1. Create command or query with handler in the Application layer (`{Verb}{Entity}Command` + `{Verb}{Entity}CommandHandler`)
2. Create validator if input needs validation (`{Verb}{Entity}CommandValidator`)
3. Add method to relevant controller — dispatch via MediatR and return `result.ToActionResult(this)`
4. Add `[Authorize]` if endpoint requires authentication
5. Write test for the handler

### Create a migration
```bash
dotnet ef migrations add MigrationName `
  --project src/CineMatch.Infrastructure `
  --startup-project src/CineMatch.API
```

### Run the project
```bash
dotnet run --project src/CineMatch.API
```
Scalar documentation is available at `/scalar` when the project is running.

### Run tests
```bash
dotnet test
```

## Things to Avoid

- ❌ Putting business logic in controllers — it belongs in handlers
- ❌ Returning entities directly from the API — always use DTOs
- ❌ Calling DbContext directly from handlers — go through repositories
- ❌ Hardcoding values that should be in appsettings
- ❌ Adding comments that describe what the code does — make the code self-explanatory instead
- ❌ Creating fat handlers that do too much — extract to domain services
- ❌ Registering services directly in Program.cs — use `DependencyInjection.cs`
- ❌ Sending raw exceptions to the client — use exception middleware
- ❌ Writing your own password hashing or JWT logic — use BCrypt and proven libraries
- ❌ Saving movies to the database without checking `TmdbId` first — avoid duplicates

## Open Questions / TBD

- Genre selection: currently hardcoded to `"popular"` in `CreateWatchPartyCommandHandler.DefaultGenre`. When real genre picking ships, the command and validator need to be reintroduced.
- Should parties be auto-closed after a period of inactivity?
- Should users be able to be in multiple active parties at the same time?

**Closed decisions:**
- Host-transfer: **not implementing** — a host cannot leave while other active members remain (`WatchPartyErrors.HostCannotLeave`). If the host is last, leaving closes the party.
- Refresh tokens: **not implementing** — 1-hour JWT lifetime is sufficient.
- Email confirmation: **not implementing** — `User.IsEmailConfirmed` defaults to `true`; users can log in immediately after registering.
- Password reset token hashing: **SHA-256** (not BCrypt) — deterministic lookup required; raw token has 256-bit entropy so BCrypt's salting adds nothing here.

## Related Documents

- `coding-standards.md` — detailed code conventions
- `docs/uml-class-diagram.md` — UML diagram of the domain model (Mermaid)
- `docs/userFlow-diagram.md` — Mermaid diagram of user flows
- `README.md` — setup instructions and overview

## Summary for Claude

When suggesting code in this project:
- Strictly follow Clean Architecture layer rules
- Use CQRS via MediatR — not direct service calls
- Write small, focused handlers
- Use DTOs for all external communication
- Hash passwords with BCrypt, never plaintext
- Validate input with FluentValidation
- Mock external dependencies in tests
- Write testable code — no hard dependencies, everything via DI
- Use NUnit for tests with `[Test]`, `[SetUp]`, and `Assert.That(...)` syntax
