# CLAUDE.md

This document provides Claude (and other AI assistants) with context about the Cinematch backend project. Read this before suggesting code, making changes, or creating new files.

## Project Overview

Cinematch is a web application where multiple users can join a "WatchParty" and swipe on movies together (Tinder-style). When all members of the party have liked the same movie, a match is created and shown to everyone. The goal is to help friends or couples find a movie everyone wants to watch without endless discussion.

This repository contains only the backend API. The frontend (React) lives in a separate repository.

## Tech Stack

- **.NET 10** (Web API)
- **Entity Framework Core** with SQL Server
- **MediatR** for CQRS
- **FluentValidation** for input validation
- **AutoMapper** for entity-to-DTO mapping
- **JWT** for authentication
- **BCrypt** for password hashing
- **Scalar** for API documentation
- **NUnit** for testing (mocking library to be decided — Moq is recommended when needed)
- **TMDB API** as external data source for movies (cached in our database)

## Architecture

We follow Clean Architecture with four projects. Dependencies always point inward — outer layers depend on inner layers, never the other way around.

```
Cinematch.API           → Cinematch.Application
Cinematch.Infrastructure → Cinematch.Application, Cinematch.Domain
Cinematch.Application   → Cinematch.Domain
Cinematch.Domain        → (no dependencies)
```

### Cinematch.Domain
- Entities, value objects, enums, domain logic
- No external dependencies (no NuGet packages beyond standard library)
- Pure C# — no EF Core, no MediatR, nothing

### Cinematch.Application
- CQRS commands and queries via MediatR
- Handlers, DTOs, interfaces (for repositories and external services)
- FluentValidation validators
- AutoMapper profiles
- Pipeline behaviours (validation, logging)

### Cinematch.Infrastructure
- EF Core DbContext and configuration
- Repository implementations (interfaces defined in Application)
- TMDB client (implements `ITmdbService` from Application)
- JWT token generation
- Migrations

### Cinematch.API
- Controllers (thin — just dispatch commands/queries via MediatR)
- Middleware (exception handling, authentication)
- Program.cs and DI configuration
- appsettings

### Tests
- `Cinematch.Tests` — separate project focused on Application handlers

## Domain Model

### Entities

**User**
- Id (Guid), Username (string), Email (string), PasswordHash (string), Role (UserRole), CreatedAt (DateTime)

**WatchParty**
- Id (Guid), JoinCode (string), HostId (Guid), Genre (string), IsActive (bool), CreatedAt (DateTime)

**PartyMember** (join table between User and WatchParty)
- Id (Guid), UserId (Guid), WatchPartyId (Guid), JoinedAt (DateTime)

**Movie** (cached from TMDB)
- Id (Guid), TmdbId (int, unique), Title (string), PosterUrl (string), Overview (string), ReleaseYear (int)

**Swipe**
- Id (Guid), PartyMemberId (Guid), WatchPartyId (Guid), MovieId (Guid), IsLiked (bool), SwipedAt (DateTime)
- Unique constraint: `(PartyMemberId, MovieId)` — a user can only swipe once per movie in the same party

**Match**
- Id (Guid), WatchPartyId (Guid), MovieId (Guid), MatchedAt (DateTime), IsWatched (bool)

### Enum

**UserRole**
- `User = 0`
- `Admin = 1`

### Relationships

- User 1 — * WatchParty (host)
- User 1 — * PartyMember and WatchParty 1 — * PartyMember (many-to-many via PartyMember)
- WatchParty 1 — * Swipe, PartyMember 1 — * Swipe, Movie 1 — * Swipe
- WatchParty 1 — * Match, Movie 1 — * Match

## Business Logic

### Match Condition
A match is created when **all active members of the party have liked the same movie**. This is evaluated every time a swipe is saved. If the number of likes on a movie equals the number of members in the party → create a Match.

### TMDB Caching
Movies are fetched from TMDB **once** when a WatchParty is created (based on the selected genre). Movies are saved in our `Movies` table using `TmdbId` as the unique identifier. If a movie already exists in the database, it is reused. All members of the same party swipe on the same movies in the same order.

### JoinCode
Generated automatically when a WatchParty is created. Should be short (e.g., 6 characters), readable, and unique among active parties.

### Host Behavior
Only the host can start the swipe session. If the host leaves the party: the role is transferred to another member, or the party is closed (TBD — decide when implementing).

## Application Layer Structure

Use CQRS with one folder per feature. Each command and query has its own folder:

```
Cinematch.Application/
  Features/
    WatchParties/
      Commands/
        CreateWatchParty/
          CreateWatchPartyCommand.cs
          CreateWatchPartyHandler.cs
          CreateWatchPartyValidator.cs
      Queries/
        GetWatchPartyById/
          GetWatchPartyByIdQuery.cs
          GetWatchPartyByIdHandler.cs
    Swipes/
      Commands/
        CreateSwipe/
          ...
    Movies/
      ...
  Common/
    Behaviours/
      ValidationBehaviour.cs
      LoggingBehaviour.cs
    Exceptions/
    Mappings/
  Interfaces/
    IWatchPartyRepository.cs
    ITmdbService.cs
  DependencyInjection.cs
```

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

### DTOs
- All client communication uses DTOs
- AutoMapper handles entity ↔ DTO mapping
- DTOs live in the Application layer

### Validation
- FluentValidation for all commands and DTOs that take input
- Validators registered as Pipeline Behavior in MediatR — runs automatically before handler

### Repositories
- Generic `IGenericRepository<T>` for standard CRUD
- Specific repositories for entity-specific logic (e.g., `IWatchPartyRepository.GetByJoinCodeAsync`)
- Repositories return entities, never DTOs
- All database logic lives in repositories — never in handlers

### Error Handling
- Custom exceptions for domain-specific errors (`WatchPartyNotFoundException`, `InvalidJoinCodeException`, etc.)
- Centralized exception middleware in the API layer
- Correct HTTP status codes: 400, 401, 403, 404, 500
- Use try/catch sparingly — let exceptions bubble up to middleware

### Authentication
- JWT with reasonable lifetime (1 hour)
- Passwords always hashed with BCrypt — never plaintext
- `[Authorize]` on protected endpoints
- `[Authorize(Roles = "Admin")]` for role-based access
- JWT secret in user secrets or appsettings — never hardcoded or committed

### Tests
- NUnit framework
- Focus on Application handlers
- Naming convention: `MethodName_Scenario_ExpectedResult`, e.g., `Handle_ValidCommand_ReturnsWatchPartyId`
- Tests must be independent and runnable in any order
- Use `[SetUp]` for per-test initialization, `[OneTimeSetUp]` for one-time setup
- Mock repositories and external services (like TMDB) — mocking library to be added when needed (Moq recommended)

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
1. Create the entity in `Cinematch.Domain/Entities`
2. Configure EF Core in `Cinematch.Infrastructure/Persistence/Configurations`
3. Add DbSet to `ApplicationDbContext`
4. Create migration: `dotnet ef migrations add AddMyEntity --project Cinematch.Infrastructure --startup-project Cinematch.API`
5. Create repository (interface in Application, implementation in Infrastructure)
6. Create DTOs and AutoMapper profile
7. Create CQRS features (commands/queries) in `Cinematch.Application/Features/MyEntity`
8. Create controller in `Cinematch.API/Controllers`
9. Write tests for handlers

### Add a new endpoint
1. Create command or query with handler in the Application layer
2. Create validator if input needs validation
3. Add method to relevant controller — dispatch via MediatR
4. Add `[Authorize]` if endpoint requires authentication
5. Write test for the handler

### Create a migration
```bash
dotnet ef migrations add MigrationName \
  --project Cinematch.Infrastructure \
  --startup-project Cinematch.API
```

### Run the project
```bash
dotnet run --project Cinematch.API
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

These decisions are not finalized yet. Update this file when they are:

- What happens when the host leaves an active party? Is the role transferred or is the party closed?
- How many movies are fetched per genre when creating a party? (suggestion: 50)
- Should parties be auto-closed after a period of inactivity?
- Should users be able to be in multiple active parties at the same time?
- Refresh tokens — implement or skip?
- Which mocking library should we adopt when we need it? (Moq is the most common choice with NUnit)

## Related Documents

- `coding-standards.md` — detailed code conventions
- `/docs/uml-class-diagram.png` — UML diagram of the domain model
- `/docs/user-flow-diagram.md` — Mermaid diagram of user flows
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
