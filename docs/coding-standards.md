# Kodregler och projektstandard - CineMatch Backend

Detta dokument beskriver de kodregler, konventioner och standarder som gäller för backend-delen av CineMatch (.NET 10 Web API). Alla i gruppen förväntas följa dessa regler för att hålla en hög och konsekvent kodkvalitet.

## Allmänna regler

### Versionshantering och Git

- All utveckling sker i feature branches, aldrig direkt på `main`.
- `main` är skyddad med branch protection rules. Ingen kan pusha direkt till main.
- Varje feature, bugfix eller task ska kopplas till en GitHub Issue.
- Pull requests krävs för all kod som mergas till `main`.
- Minst en annan gruppmedlem ska godkänna en PR innan merge.

### Branch-namngivning

Använd följande prefix för branch-namn:

- `feature/` för nya funktioner, t.ex. `feature/jwt-authentication`
- `bugfix/` för buggfixar, t.ex. `bugfix/match-detection-null-reference`
- `refactor/` för refaktorering, t.ex. `refactor/move-validation-to-handler`
- `docs/` för dokumentation, t.ex. `docs/update-readme`
- `test/` för tester, t.ex. `test/add-match-handler-tests`

Använd kebab-case och beskrivande namn. Inga personnamn eller datum i branch-namn.

### Commit-meddelanden

Skriv meningsfulla commit-meddelanden i imperativ form på engelska:

- Bra: `Add JWT authentication to UserController`
- Bra: `Fix null reference in MatchDetectionService`
- Dåligt: `fixed stuff`
- Dåligt: `wip`

Håll commits små och fokuserade. En commit ska göra en sak.

### GitHub Project Board

- Alla tasks ska finnas som issues på Project Boarden.
- Issues ska ha tydliga beskrivningar och acceptanskriterier.
- Använd labels för att kategorisera (backend, bug, feature, docs, test).
- Flytta issues mellan kolumner (Todo, In Progress, Review, Done) under arbetets gång.
- Tilldela issues till den som arbetar med dem.

## Arkitektur

Vi följer Clean Architecture med fyra projekt. Produktionsprojekten ligger under `src/`, testprojektet i repo-roten.

```
CineMatch-Backend/
  src/
    CineMatch.API/
    CineMatch.Application/
    CineMatch.Infrastructure/
    CineMatch.Domain/
  CineMatch.Tests/
```

- `CineMatch.Domain` - entiteter, enums, domänlogik. Inga externa beroenden.
- `CineMatch.Application` - CQRS commands/queries, handlers, DTOs, interfaces, validering, AutoMapper-profiler, pipeline behaviours.
- `CineMatch.Infrastructure` - EF Core (`AppDbContext`), repositories, externa tjänster (TMDB), JWT, BCrypt-hashing, migrations.
- `CineMatch.API` - controllers, middleware, DI-konfiguration, Program.cs, Scalar-dokumentation.

Beroenden går alltid inåt: API → Application → Domain. Infrastructure beror på Application och Domain men aldrig tvärtom.

## Namnkonventioner

- Klasser, metoder, properties: `PascalCase`
- Privata fält: `_camelCase` med understreck
- Lokala variabler och parametrar: `camelCase`
- Konstanter: `PascalCase`
- Interfaces: `IPascalCase` (alltid med I-prefix)
- Async-metoder: alltid suffix `Async`, t.ex. `GetMoviesAsync`

### Naming för Commands, Queries, Handlers och Validators

Filnamnen ska göra det glasklart vilken typ klassen är. Vi behåller alltid suffixet `Command`/`Query` även på handlern och validatorn.

| Typ       | Mönster                              | Exempel                              |
|-----------|--------------------------------------|--------------------------------------|
| Command   | `{Verb}{Entity}Command`              | `RegisterUserCommand`                |
| Query     | `{Verb}{Entity}Query`                | `GetUserByIdQuery`                   |
| Handler   | `{CommandOrQueryName}Handler`        | `RegisterUserCommandHandler`         |
| Validator | `{CommandOrQueryName}Validator`      | `RegisterUserCommandValidator`       |
| DTO       | `{Entity}Dto` (eller actionspecifik response när formen är unik) | `UserDto` |
| Errors    | `{Entity}Errors` (statisk klass med `Error`-properties) | `UserErrors`       |

Regler:

- Korta inte ner till `RegisterUserHandler` eller `RegisterUserValidator` — `Command`/`Query`-suffixet ska alltid vara med.
- Mappnamnet matchar request-namnet utan suffixet (mappen `RegisterUser/` innehåller `RegisterUserCommand.cs` + `RegisterUserCommandHandler.cs` + `RegisterUserCommandValidator.cs`).
- Interfaces för repositories följer `I{Entity}Repository`, t.ex. `IUserRepository`.
- Externa tjänster (Infrastructure-implementationer) namnges efter implementationsdetaljen, t.ex. `BCryptPasswordHasher`, `JwtService`.

## Struktur i Application-lagret

Använd CQRS-mönstret med tydligt separerade Commands och Queries. Varje feature har sin egen mapp under `Features/`. DTOs och felklasser för entiteten ligger under `Common/` inom samma feature-mapp.

```
CineMatch.Application/
  Features/
    Users/
      Commands/
        RegisterUser/
          RegisterUserCommand.cs
          RegisterUserCommandHandler.cs
          RegisterUserCommandValidator.cs
      Queries/
        GetUserById/
          GetUserByIdQuery.cs
          GetUserByIdQueryHandler.cs
      Common/
        Dtos/
          UserDto.cs
        Errors/
          UserErrors.cs
        Mappings/
          UserMappingProfile.cs
    WatchParties/
      Commands/
      Queries/
      Common/
        Dtos/
        Errors/
        Mappings/
  Common/
    Behaviours/
      ValidationBehaviour.cs
      LoggingBehaviour.cs
  Services/
    MatchDetectionService.cs
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
  DependencyInjection.cs
```

AutoMapper-profiler ligger per feature under `Features/{Entity}/Common/Mappings/{Entity}MappingProfile.cs` och plockas upp automatiskt — ingen central `MappingProfile.cs`. Interfaces är uppdelade i `Interfaces/Repositories/` och `Interfaces/Services/` så det är tydligt vad som är dataåtkomst och vad som är tjänst.

## Allmänna kodregler

- En klass per fil. Filnamnet matchar klassnamnet.
- Inga magic numbers eller magic strings. Använd konstanter eller enums.
- Undvik kommentarer som beskriver vad koden gör. Skriv koden så att den är självförklarande. Kommentarer ska användas för att förklara varför, inte vad.
- Max ca 20-30 rader per metod. Bryt ut längre metoder.
- Använd dependency injection överallt. Inga `new`-instansieringar av services i klasser.

## Felhantering

Vi använder `ErrorOr` Result-mönstret för förväntade applikationsfel — inte custom exceptions.

- Handlers returnerar `ErrorOr<T>` från MediatR. Vid fel returnera ett `Error`-värde, vid framgång ett `T`.
- Definiera återanvändbara fel som statiska `Error`-properties i en per-entitet-klass under `Features/{Entity}/Common/Errors/`, t.ex. `UserErrors.EmailAlreadyExists`.
- **Skapa inte** custom exceptions för normala flöden (t.ex. `WatchPartyNotFoundException`, `InvalidJoinCodeException`).
- Validator-fel konverteras automatiskt till `Error.Validation(...)` av `ValidationBehaviour<TRequest, TResponse>` (kräver att responsen implementerar `IErrorOr`).
- Controllers är tunna — dispatcha via MediatR och mappa `ErrorOr<T>` till `IActionResult` med extension-metoden `result.ToActionResult(this)` i `CineMatch.API/Common/ResultExtensions.cs`.
- Mappning: `Validation` → 400, `Unauthorized` → 401, `Forbidden` → 403, `NotFound` → 404, `Conflict` → 409, övrigt → 500.
- `ExceptionHandlingMiddleware` i API-lagret fångar bara **oväntade** exceptions och returnerar `ErrorResponse`-kontraktet. Använd try/catch sparsamt — låt oväntade exceptions bubbla upp till middleware.

## DTOs

- All kommunikation mellan API och klient sker via DTOs, aldrig via entiteter direkt.
- Använd AutoMapper för att mappa mellan entiteter och DTOs. Mappningsprofiler ligger per feature under `Features/{Entity}/Common/Mappings/{Entity}MappingProfile.cs` och plockas upp automatiskt via `services.AddAutoMapper(cfg => cfg.AddMaps(...))` — ingen manuell registrering behövs.
- För positional record-DTOs, använd `.ForCtorParam(...)` istället för `.ForMember(...)`.
- DTOs ligger per feature i `Features/{Entity}/Common/Dtos/`, inte i en global `Dtos/`-mapp.
- Namnge DTOs efter användningsområde, t.ex. `UserDto`, `WatchPartyDto`, `WatchPartyDetailDto`.

## Validering

- Använd FluentValidation för alla commands och DTOs som tar input från klienten.
- Validatorer ligger i samma mapp som tillhörande command och följer namnet `{CommandName}Validator` (t.ex. `RegisterUserCommandValidator`).
- Validering körs automatiskt som pipeline behavior i MediatR (`ValidationBehaviour<,>`) — handlern triggas inte om validering misslyckas.
- Validatorer registreras automatiskt via `AddValidatorsFromAssembly` i Application-lagrets `DependencyInjection.cs`.

## Repositories

- Använd Repository Pattern. Generisk `IGenericRepository<T>` täcker vanliga CRUD-operationer (`GetByIdAsync`, `GetAllAsync`, `AddAsync`, `Update`, `Delete`) och registreras som öppen generic i Infrastructure-DI.
- Skapa specifika repositories för entitetsspecifik logik genom att ärva från `GenericRepository<T>` och implementera ett eget interface, t.ex. `IUserRepository : IGenericRepository<User>` med `GetByEmailAsync`, `ExistsByEmailAsync`.
- Repositories returnerar entiteter, aldrig DTOs.
- All databaslogik ska ligga i repositories, inte i handlers.
- Anropa **aldrig** `DbContext.SaveChanges` direkt i handlers eller repositories — använd `IUnitOfWork.SaveChangesAsync()` för att persistera ändringar.
- EF Core-konfiguration ligger i `CineMatch.Infrastructure/Database/Configurations/` (t.ex. `UserConfiguration : IEntityTypeConfiguration<User>`) och plockas upp automatiskt av `AppDbContext` via `ApplyConfigurationsFromAssembly`.

## Pipeline Behaviours

- Implementera pipeline behaviours i MediatR för cross-cutting concerns.
- `LoggingBehaviour<,>` och `ValidationBehaviour<,>` ska finnas registrerade.
- Behaviours registreras i `DependencyInjection.cs` i Application-lagret som `IPipelineBehavior<,>`.

## Applikationsservices

För domänlogik som spänner över flera repositories (t.ex. match-detektering) eller delas mellan flera handlers, skapa en applikationsservice under `CineMatch.Application/Services/`:

- Interfacet ligger i `Application/Interfaces/Services/I{Name}Service.cs`.
- Implementationen ligger i `Application/Services/{Name}Service.cs` — i Application-lagret, inte Infrastructure, eftersom servicen inte har externa beroenden utan bara domänlogik ovanpå repositories.
- Servicen sparar inte själv. `SaveChangesAsync` ägs av handlern som anropar den; servicen returnerar entiteter eller resultat som handlern persisterar i samma transaktion.
- Registreras i `Application/DependencyInjection.cs`.

Externa tjänster (TMDB, JWT, BCrypt) ligger däremot i `Infrastructure/Services/` eftersom de har externa beroenden.

## Externa tjänster (HTTP-integration)

- Externa API-anrop sker via typed `HttpClient` registrerade i `Infrastructure/DependencyInjection.cs`:
  ```csharp
  services.AddHttpClient<ITmdbService, TmdbService>(client =>
      client.BaseAddress = new Uri(tmdbSettings.BaseUrl));
  ```
- Interfacet definieras i Application-lagret (`Interfaces/Services/`), implementationen ligger i Infrastructure (`Services/`).
- Konfiguration läses via strongly-typed settings (`IOptions<TmdbSettings>`) med samma mönster som `JwtSettings`. Hemligheter (API-nycklar, tokens) sätts via `dotnet user-secrets`, aldrig hårdkodat eller committat.
- Externa anrop ska kunna mockas i tester. Mocka `HttpMessageHandler` (via NSubstitute eller en `TestHttpMessageHandler`-helper) — slå aldrig mot riktiga externa tjänster i tester.

## Authentication och Authorization

- Använd JWT för authentication. Tokens genereras i `JwtService` (Infrastructure) bakom interfacet `IJwtService` (Application).
- Lösenord ska alltid hashas med BCrypt via `IPasswordHasher` / `BCryptPasswordHasher`. Aldrig spara lösenord i klartext.
- Använd `[Authorize]`-attribut på protected endpoints.
- Använd `[Authorize(Roles = "Admin")]` för rollbaserad åtkomst (rollerna definieras av enumen `UserRole`).
- Tokens ska ha rimlig livslängd (t.ex. 1 timme). Refresh tokens är TBD — implementera när det beslutats.
- JWT-secret och andra känsliga värden ligger i appsettings eller user secrets, aldrig hårdkodat eller committat. Hemligheten ska vara minst 32 tecken — `AddJwtAuthentication` validerar detta vid uppstart.

## Tester

- Tester ligger i ett separat projekt: `CineMatch.Tests`.
- Fokusera på handlers i Application-lagret.
- Använd NUnit (`[Test]`, `[SetUp]`, `[OneTimeSetUp]`, `Assert.That(...)`).
- Namnge tester enligt mönstret `MethodName_Scenario_ExpectedResult`, t.ex. `Handle_ValidCommand_ReturnsUserId`.
- Varje test ska vara oberoende och kunna köras i valfri ordning.
- Mocka repositories och externa tjänster (t.ex. TMDB). NSubstitute används för mocking.
- Sträva efter att alla CRUD-flöden för alla entiteter har tester (VG-krav).

## Dependency Injection

- Använd en separat `DependencyInjection.cs`-fil per projekt (Application, Infrastructure) för service-registreringar.
- Program.cs ska bara anropa extension-metoderna, inte registrera services direkt.
- Exempel:
  ```csharp
  builder.Services.AddInfrastructure(builder.Configuration);
  builder.Services.AddApplication();
  ```

## Vanliga kommandon

```bash
# Kör API:t
dotnet run --project src/CineMatch.API

# Kör tester
dotnet test

# Skapa en migration
dotnet ef migrations add MigrationName `
  --project src/CineMatch.Infrastructure `
  --startup-project src/CineMatch.API
```

API-dokumentationen (Scalar) finns på `/scalar` när projektet körs.

## Dokumentation

### README

Repot ska ha en README som innehåller:

- Projektets namn och kort beskrivning
- Tekniker och bibliotek som används (.NET 10, EF Core med Npgsql, MediatR, ErrorOr, FluentValidation, AutoMapper, BCrypt, JWT, Scalar)
- Förutsättningar (.NET 10 SDK, PostgreSQL)
- Steg-för-steg-instruktioner för att starta projektet lokalt, inklusive migrations
- Hur man kör tester
- Hur man når API-dokumentation (Scalar på `/scalar`)
- Länk till frontend-repot

### API-dokumentation

- Använd Scalar för att dokumentera alla endpoints (registreras via `MapScalarApiReference` i Development).
- Alla endpoints ska ha tydliga beskrivningar, parametrar och exempel-responses.
- Markera vilka endpoints som kräver autentisering.

### Diagram

- UML Class Diagram ska finnas i `/docs/uml-class-diagram.png` (eller `.md` om Mermaid används).
- User Flow Diagram ska finnas i `docs/userFlow-diagram.md`.
- Båda diagrammen ska länkas från README.

## Code Review

Vid pull requests ska reviewern kontrollera:

- Följs namnkonventioner och kodstil (inkl. `Command`/`Query`-suffix på handlers och validators)?
- Är koden läsbar och självförklarande?
- Finns relevanta tester?
- Hanteras fel korrekt med `ErrorOr` (inte custom exceptions)?
- Är commit-historiken ren och meningsfull?
- Bryts Clean Architecture lager-regler någonstans?
- Finns onödig kod, console-utskrifter eller kommenterad kod?

Reviewer ska ge konstruktiv feedback. Den som gjort PR:en åtgärdar feedback innan merge.

## Sammanfattning

Vi prioriterar läsbarhet, struktur och teamwork framför att skriva mycket kod snabbt. Bättre att stanna upp och diskutera ett designval än att merga något ingen förstår. Om du är osäker på något, fråga gruppen istället för att gissa.
