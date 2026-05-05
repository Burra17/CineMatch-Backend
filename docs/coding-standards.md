# Kodregler och projektstandard - Cinematch Backend

Detta dokument beskriver de kodregler, konventioner och standarder som gäller för backend-delen av Cinematch (.NET Core API). Alla i gruppen förväntas följa dessa regler för att hålla en hög och konsekvent kodkvalitet.

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

Vi följer Clean Architecture med fyra projekt:

- `Cinematch.Domain` - entiteter, enums, domänlogik. Inga externa beroenden.
- `Cinematch.Application` - CQRS commands/queries, handlers, DTOs, interfaces, validering.
- `Cinematch.Infrastructure` - EF Core, repositories, externa tjänster (TMDB), JWT.
- `Cinematch.API` - controllers, middleware, DI-konfiguration, Program.cs.

Beroenden går alltid inåt: API → Application → Domain. Infrastructure beror på Application och Domain men aldrig tvärtom.

## Namnkonventioner

- Klasser, metoder, properties: `PascalCase`
- Privata fält: `_camelCase` med understreck
- Lokala variabler och parametrar: `camelCase`
- Konstanter: `PascalCase`
- Interfaces: `IPascalCase` (alltid med I-prefix)
- Async-metoder: alltid suffix `Async`, t.ex. `GetMoviesAsync`

## Struktur i Application-lagret

Använd CQRS-mönstret med tydligt separerade Commands och Queries:

```
Application/
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
```

Varje feature har sin egen mapp. Varje command och query har sin egen mapp med tillhörande handler och validator.

## Allmänna kodregler

- En klass per fil. Filnamnet matchar klassnamnet.
- Inga magic numbers eller magic strings. Använd konstanter eller enums.
- Undvik kommentarer som beskriver vad koden gör. Skriv koden så att den är självförklarande. Kommentarer ska användas för att förklara varför, inte vad.
- Max ca 20-30 rader per metod. Bryt ut längre metoder.
- Använd dependency injection överallt. Inga `new`-instansieringar av services i klasser.

## Felhantering

- Använd custom exceptions för domänspecifika fel (t.ex. `WatchPartyNotFoundException`, `InvalidJoinCodeException`).
- Kasta exceptions i Application-lagret när något går fel.
- Hantera exceptions centralt i en exception middleware i API-lagret.
- Returnera tydliga felmeddelanden och korrekta HTTP-statuskoder (400, 401, 403, 404, 500).
- Använd try/catch där det är meningsfullt, inte överallt. Låt exceptions bubbla upp till middleware.

## DTOs

- All kommunikation mellan API och klient sker via DTOs, aldrig via entiteter direkt.
- Använd AutoMapper för att mappa mellan entiteter och DTOs.
- DTOs ligger i Application-lagret.
- Namnge DTOs efter användningsområde, t.ex. `WatchPartyDto`, `CreateWatchPartyDto`, `WatchPartyDetailDto`.

## Validering

- Använd FluentValidation för alla commands och DTOs som tar input från klienten.
- Validatorer ligger i samma mapp som tillhörande command.
- Registrera validering som ett pipeline behavior i MediatR så att validering körs automatiskt innan handlern.

## Repositories

- Använd Repository Pattern. Skapa en generisk `IGenericRepository<T>` för vanliga CRUD-operationer.
- Skapa specifika repositories för entitetsspecifik logik, t.ex. `IWatchPartyRepository` med metod `GetByJoinCodeAsync`.
- Repositories returnerar entiteter, aldrig DTOs.
- All databaslogik ska ligga i repositories, inte i handlers.

## Pipeline Behaviours

- Implementera pipeline behaviours i MediatR för cross-cutting concerns.
- Minst en validation behavior och en logging behavior ska finnas.
- Behaviours registreras i `DependencyInjection.cs` i Application-lagret.

## Authentication och Authorization

- Använd JWT för authentication.
- Lösenord ska alltid hashas med BCrypt eller liknande. Aldrig spara lösenord i klartext.
- Använd `[Authorize]`-attribut på protected endpoints.
- Använd `[Authorize(Roles = "Admin")]` för rollbaserad åtkomst.
- Tokens ska ha rimlig livslängd (t.ex. 1 timme) och stödja refresh tokens om möjligt.
- JWT-secret och andra känsliga värden ligger i appsettings eller user secrets, aldrig hårdkodat.

## Tester

- Tester ligger i ett separat projekt: `Cinematch.Tests`.
- Fokusera på handlers i Application-lagret.
- Använd NUnit.
- Namnge tester enligt mönstret `MethodName_Scenario_ExpectedResult`, t.ex. `Handle_ValidCommand_ReturnsWatchPartyId`.
- Varje test ska vara oberoende och kunna köras i valfri ordning.
- Sträva efter att alla CRUD-flöden för alla entiteter har tester (VG-krav).

## Dependency Injection

- Använd en separat `DependencyInjection.cs`-fil per projekt (Application, Infrastructure) för service-registreringar.
- Program.cs ska bara anropa extension-metoderna, inte registrera services direkt.
- Exempel: `builder.Services.AddApplication();` och `builder.Services.AddInfrastructure(builder.Configuration);`

## Dokumentation

### README

Repot ska ha en README som innehåller:

- Projektets namn och kort beskrivning
- Tekniker och bibliotek som används (.NET 8, EF Core, MediatR, FluentValidation, etc.)
- Förutsättningar (t.ex. .NET 8 SDK, SQL Server eller motsvarande)
- Steg-för-steg-instruktioner för att starta projektet lokalt, inklusive migrations
- Hur man kör tester
- Hur man når API-dokumentation (Swagger eller Scalar)
- Länk till frontend-repot

### API-dokumentation

- Använd Swagger eller Scalar för att dokumentera alla endpoints.
- Alla endpoints ska ha tydliga beskrivningar, parametrar och exempel-responses.
- Markera vilka endpoints som kräver autentisering.

### Diagram

- UML Class Diagram ska finnas i `/docs/uml-class-diagram.png` (eller `.md` om Mermaid används).
- User Flow Diagram ska finnas i `/docs/user-flow-diagram.png` (eller `.md` om Mermaid används).
- Båda diagrammen ska länkas från README.

## Code Review

Vid pull requests ska reviewern kontrollera:

- Följs namnkonventioner och kodstil?
- Är koden läsbar och självförklarande?
- Finns relevanta tester?
- Hanteras fel korrekt?
- Är commit-historiken ren och meningsfull?
- Bryts Clean Architecture lager-regler någonstans?
- Finns onödig kod, console-utskrifter eller kommenterad kod?

Reviewer ska ge konstruktiv feedback. Den som gjort PR:en åtgärdar feedback innan merge.

## Sammanfattning

Vi prioriterar läsbarhet, struktur och teamwork framför att skriva mycket kod snabbt. Bättre att stanna upp och diskutera ett designval än att merga något ingen förstår. Om du är osäker på något, fråga gruppen istället för att gissa.
