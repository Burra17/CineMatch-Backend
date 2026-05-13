# CineMatch Backend — Roadmap

Detta dokument samlar **allt som återstår i backend-repot** så hela teamet ser planen på ett ställe. Detaljerade tasks per arbete ligger som GitHub-issues på [Project Board #2](https://github.com/users/Burra17/projects/2); den här filen är roadmapen.

## Status

| Sprint | Status | Innehåll |
|--------|--------|----------|
| Sprint 1 | ✅ Klar | Foundation, User-entitet, JWT-auth, BCrypt, repositories |
| Sprint 2 | ✅ Klar | Auth-flöden (Register, Login, GetCurrentUser), pipeline behaviours, tester |
| Sprint 3 | ✅ Klar | WatchParty, PartyMember, Movie, JoinCodeGenerator, soft delete |
| **Sprint 4** | 🚧 Pågående | TMDB-integration + Swipe + Match |
| Sprint 5 | ⏳ Återstår | Password reset + polish + tester för VG |
| Sprint 6 | ⏳ Frontend | Egen repo (React) — startar när backend är klar |

---

## Sprint 4 — TMDB-integration + Swipe + Match

### Mål

Stänga de två luckorna efter Sprint 3:
1. Integrera TMDB så att filmer hämtas och cachas vid party-skapande.
2. Implementera Swipe- och Match-flödet end-to-end.

### Designbeslut

- **`WatchPartyMovie` join-entitet** införs som länk mellan `WatchParty` och `Movie` med ett `OrderIndex`. Detta ger oss en deterministisk filmordning som alla medlemmar i partyt ser, utan att förlora normalisering eller referensintegritet. Alternativen var EF many-to-many utan ordning (förlorar ordningen) och JSON-array av TmdbIds på `WatchParty` (bryter normalisering).
- **`MatchDetectionService`** läggs i `Application/Services/` — en ny mapp för applikationsservices som inte hör hemma i Infrastructure (de innehåller domänlogik, inte externa beroenden).
- **`MoviesPerParty = 50`** som konstant i `CreateWatchPartyCommandHandler`. Flyttas till `TmdbSettings` när det blir konfigurerbart.
- **Tester paketeras i samma issue som handlern** (avviker från Sprint 3-mönstret med separata test-issues, men håller scope på 13 issues istället för 17).

### Issue-översikt

> När issues skapas på GitHub uppdateras denna tabell med #-nummer.

| ID | Titel | Beroenden | Issue |
|----|-------|-----------|-------|
| S4-01 | Skapa `WatchPartyMovie`-entitet + EF-konfiguration | — | [#87](https://github.com/Burra17/CineMatch-Backend/issues/87) |
| S4-02 | Skapa `Swipe`-entitet + EF-konfiguration | — | [#88](https://github.com/Burra17/CineMatch-Backend/issues/88) |
| S4-03 | Skapa `Match`-entitet + EF-konfiguration | — | [#89](https://github.com/Burra17/CineMatch-Backend/issues/89) |
| S4-04 | Migration: `AddSwipeMatchAndWatchPartyMovie` | S4-01, S4-02, S4-03 | [#90](https://github.com/Burra17/CineMatch-Backend/issues/90) |
| S4-05 | `TmdbSettings` + `ITmdbService` + `TmdbService` | — | [#91](https://github.com/Burra17/CineMatch-Backend/issues/91) |
| S4-06 | Repositories för `Swipe`, `Match`, `WatchPartyMovie` | S4-04 | [#92](https://github.com/Burra17/CineMatch-Backend/issues/92) |
| S4-07 | Integrera TMDB i `CreateWatchPartyCommand` | S4-05, S4-06 | [#93](https://github.com/Burra17/CineMatch-Backend/issues/93) |
| S4-08 | `MatchDetectionService` | S4-06 | [#94](https://github.com/Burra17/CineMatch-Backend/issues/94) |
| S4-09 | `CreateSwipeCommand` + handler + validator | S4-06, S4-08 | [#95](https://github.com/Burra17/CineMatch-Backend/issues/95) |
| S4-10 | `GetSwipeQueueQuery` + handler | S4-06 | [#96](https://github.com/Burra17/CineMatch-Backend/issues/96) |
| S4-11 | `GetMatchesByPartyQuery` + `MarkMatchAsWatchedCommand` | S4-06 | [#97](https://github.com/Burra17/CineMatch-Backend/issues/97) |
| S4-12 | `SwipesController` + `MatchesController` | S4-09, S4-10, S4-11 | [#98](https://github.com/Burra17/CineMatch-Backend/issues/98) |
| S4-13 | Uppdatera `CLAUDE.md`, UML och userFlow | S4-12 | [#99](https://github.com/Burra17/CineMatch-Backend/issues/99) |

Fulla utkast (Beskrivning, Tasks, Acceptanskriterier) finns i [bilagan längst ner](#bilaga-utkast-till-sprint-4-issues).

---

## Sprint 5 — Reset Password + Polish + Tester

### Mål

VG-kraven kräver tester för alla CRUD-flöden och fullt fungerande password reset. Sprint 5 fokuserar på det plus rengöring av öppna frågor.

### Tasks

1. **`PasswordResetToken`-entitet + EF-konfiguration + migration**
   - Fält: `Id`, `UserId`, `TokenHash`, `ExpiresAt`, `IsUsed`, `CreatedAt`, `UsedAt`
   - Unique constraint på `TokenHash`
2. **`RequestPasswordResetCommand` + handler**
   - Genererar token, hashar det, sparar `PasswordResetToken`-rad
   - I dev: returnera token direkt i responsen (sluten miljö)
   - Produktion (TBD): skicka via mejl
3. **`ResetPasswordCommand` + handler + validator**
   - Validerar token (hash-matchning, ej utgången, ej använd)
   - Sätter nytt lösenord via `IPasswordHasher`
   - Markerar token `IsUsed=true`, `UsedAt=UtcNow`
4. **Reset Password-endpoints i `AuthController`**
   - `POST /api/Auth/forgot-password`
   - `POST /api/Auth/reset-password`
5. **Komplettera tester för alla CRUD-flöden** (VG-krav)
   - Audit befintliga tester per entitet — saknas något flöde, lägg till
   - Targets: User, WatchParty, PartyMember, Swipe, Match, PasswordResetToken
6. **Integrationstester för auth-flödet** (frivilligt — kan klippas)
7. **Refaktorera CancellationToken-policy**
   - Lägg till `CancellationToken` på `IGenericRepository`-metoder där det saknas
   - Säkerställ att alla handlers skickar vidare token från MediatR
8. **Seed-data för development** (frivilligt)
   - Skapa en eller två test-users vid uppstart i `Development`
9. **Logging för produktion** (Serilog eller motsv — frivilligt, kan klippas)
10. **Felhantering för edge cases** upptäckta under Sprint 4-utveckling
11. **Polera README och dokumentation**
12. **Uppdatera CLAUDE.md med slutgiltiga beslut**
    - Stäng alla "Open Questions / TBD"-punkter
    - Dokumentera password reset-flödet

---

## Sprint 6 — Frontend (egen repo)

Frontend startar i en egen repo när backend är komplett. Punkter ingår här bara för helhetsbilden:

1. Setup React (Vite + Tailwind + React Router)
2. Axios + JWT-interceptor
3. AuthContext + ProtectedRoute
4. Login + Register-sidor
5. Reset Password-flöde
6. Dashboard
7. Skapa party / Gå med-sidor
8. Lobby
9. Swipe-vy med filmkort
10. Match-vy
11. Historik-sida
12. Anslut alla endpoints + felhantering
13. Frontend README
14. Demo-prep

---

## Tvärgående under hela projektet

- Code review på alla PR:s (minst en gruppmedlem godkänner)
- Hålla `CLAUDE.md` uppdaterad
- Hålla tester gröna (CI körs via `.github/workflows/build-and-test.yml`)
- UML och userFlow synkade med koden

---

## Kan klippas om tiden tryter

Följande kan utelämnas utan att tappa VG-poäng:

- Refresh tokens (kortare JWT räcker)
- Email-verifiering (`IsEmailConfirmed` defaultar till `true`)
- Auto-stängning av inaktiva parties
- Email-utskick för reset password (returnera token i response för dev)
- Admin-funktionalitet (om inte uttryckligen krävs)
- Integrationstester (om unit tests täcker)
- Seed-data
- Avancerad logging (default räcker)

---

## Bilaga: Utkast till Sprint 4-issues

Varje utkast följer Sprint 3-stilen: Beskrivning + Tasks + Acceptanskriterier + Beroenden. När ett utkast godkänns och skapas som GitHub-issue ersätts `TBD` i tabellen ovan med issue-numret.

---

### S4-01 — Skapa `WatchPartyMovie`-entitet + EF-konfiguration

**Labels:** `backend`, `entity`, `database`, `sprint-4`

**Beskrivning**

Ny join-entitet som länkar `WatchParty` ↔ `Movie` med en explicit ordning. Behövs för att alla medlemmar i samma party ska swipa på samma filmer i samma ordning. Beslutat efter design-diskussion (alternativen var EF many-to-many utan ordning eller JSON-array av TmdbIds).

**Tasks**

- [ ] Skapa `WatchPartyMovie.cs` i `src/CineMatch.Domain/Models`:
  - `Id` (Guid, init), `WatchPartyId` (Guid), `MovieId` (Guid), `OrderIndex` (int), `AddedAt` (DateTime, init)
- [ ] Lägg till navigation properties:
  - `WatchPartyMovie.WatchParty`, `WatchPartyMovie.Movie`
  - `WatchParty.WatchPartyMovies` (`ICollection<WatchPartyMovie>`)
- [ ] Skapa `WatchPartyMovieConfiguration.cs` i `src/CineMatch.Infrastructure/Database/Configurations`:
  - Unique constraint: `(WatchPartyId, MovieId)`
  - Unique constraint: `(WatchPartyId, OrderIndex)`
  - Relations: `WatchParty` (Cascade), `Movie` (Restrict)
- [ ] Lägg till `DbSet<WatchPartyMovie>` i `AppDbContext`

**Acceptanskriterier**

- [ ] Entiteten finns i `Domain/Models` utan externa beroenden
- [ ] EF-konfigurationen finns och plockas upp av `ApplyConfigurationsFromAssembly`
- [ ] Båda unique constraints verifierade i konfigurationen
- [ ] Inga magic strings i config

---

### S4-02 — Skapa `Swipe`-entitet + EF-konfiguration

**Labels:** `backend`, `entity`, `database`, `sprint-4`

**Beskrivning**

Domänentitet för en swipe (like/dislike) som en `PartyMember` gör på en `Movie` inom ett party.

**Tasks**

- [ ] Skapa `Swipe.cs` i `src/CineMatch.Domain/Models`:
  - `Id` (Guid, init), `PartyMemberId` (Guid), `WatchPartyId` (Guid), `MovieId` (Guid), `IsLiked` (bool), `SwipedAt` (DateTime, init)
- [ ] Navigation properties: `Swipe.PartyMember`, `Swipe.WatchParty`, `Swipe.Movie`
- [ ] `PartyMember.Swipes`, `WatchParty.Swipes`, `Movie.Swipes` (`ICollection<Swipe>`)
- [ ] Skapa `SwipeConfiguration.cs`:
  - Unique constraint på `(PartyMemberId, MovieId)`
  - Relations: `PartyMember` (Restrict), `WatchParty` (Cascade), `Movie` (Restrict)
- [ ] Lägg till `DbSet<Swipe>` i `AppDbContext`

**Acceptanskriterier**

- [ ] Entitet + config skapade
- [ ] Unique constraint matchar UML-diagrammet och CLAUDE.md
- [ ] DeleteBehavior valt medvetet per relation (motivera i PR-beskrivningen)

---

### S4-03 — Skapa `Match`-entitet + EF-konfiguration

**Labels:** `backend`, `entity`, `database`, `sprint-4`

**Beskrivning**

Domänentitet för en match (alla aktiva medlemmar i partyt har likat samma film).

> **Notera:** UML-diagrammet har `WatchedByUserId` och `WatchedAt` på `Match`, vilket CLAUDE.md inte har. Inkludera dessa fält — `MarkMatchAsWatchedCommand` (S4-11) sätter dem. CLAUDE.md uppdateras i S4-13.

**Tasks**

- [ ] Skapa `Match.cs` i `src/CineMatch.Domain/Models`:
  - `Id` (Guid, init), `WatchPartyId` (Guid), `MovieId` (Guid), `MatchedAt` (DateTime, init), `IsWatched` (bool), `WatchedByUserId` (Guid?), `WatchedAt` (DateTime?)
- [ ] Navigation properties: `Match.WatchParty`, `Match.Movie`, `Match.WatchedBy` (User?)
- [ ] `WatchParty.Matches`, `Movie.Matches`
- [ ] Skapa `MatchConfiguration.cs`:
  - Unique constraint på `(WatchPartyId, MovieId)`
  - Relations: `WatchParty` (Cascade), `Movie` (Restrict), `WatchedBy` (Restrict, optional)
  - `HasDefaultValue(false)` på `IsWatched`
- [ ] Lägg till `DbSet<Match>` i `AppDbContext`

**Acceptanskriterier**

- [ ] Entitet + config skapade
- [ ] `WatchedBy` är optional (nullable FK)
- [ ] Unique constraint på `(WatchPartyId, MovieId)` verifierad

---

### S4-04 — Migration: `AddSwipeMatchAndWatchPartyMovie`

**Labels:** `backend`, `database`, `sprint-4`

**Beskrivning**

Skapa och kör EF Core-migration för alla tre nya tabellerna i ett steg.

**Tasks**

- [ ] Kör migration-kommando:
  ```powershell
  dotnet ef migrations add AddSwipeMatchAndWatchPartyMovie `
    --project src/CineMatch.Infrastructure `
    --startup-project src/CineMatch.API
  ```
- [ ] Granska den genererade migrationen — tre `CreateTable`-anrop, indexes, foreign keys
- [ ] Kör `dotnet ef database update`
- [ ] Verifiera tabellerna i PostgreSQL (`\dt`)
- [ ] Verifiera unique constraints (`\d swipes`, `\d matches`, `\d watch_party_movies`)

**Acceptanskriterier**

- [ ] Migration-fil committad under `src/CineMatch.Infrastructure/Migrations/`
- [ ] Tre nya tabeller finns i databasen
- [ ] Alla unique constraints från S4-01, S4-02, S4-03 verifierade
- [ ] Foreign keys pekar rätt

**Beroenden:** S4-01, S4-02, S4-03

---

### S4-05 — `TmdbSettings` + `ITmdbService` + `TmdbService`

**Labels:** `backend`, `service`, `infrastructure`, `sprint-4`

**Beskrivning**

Sätt upp HTTP-integration mot TMDB API. Strongly-typed settings, `HttpClient` via `IHttpClientFactory`, interface i Application-lagret, implementation i Infrastructure.

**Tasks**

- [ ] Skapa `TmdbSettings.cs` i `src/CineMatch.Infrastructure/Database/Configurations` (samma konvention som `JwtSettings`):
  - `SectionName = "Tmdb"`
  - `ApiKey` (string), `BaseUrl` (string), `DefaultLanguage` (string = "en-US")
- [ ] Lägg till `Tmdb`-sektion i `appsettings.json` (utan ApiKey)
- [ ] Skapa `ITmdbService` i `src/CineMatch.Application/Interfaces/Services`:
  - `Task<IReadOnlyList<TmdbMovieDto>> GetMoviesByGenreAsync(string genre, int count, CancellationToken cancellationToken)`
- [ ] Skapa `TmdbMovieDto` under `src/CineMatch.Application/Features/Movies/Common/Dtos/`
  - Fält: `TmdbId`, `Title`, `PosterUrl`, `Overview`, `ReleaseYear`
- [ ] Skapa `TmdbService` i `src/CineMatch.Infrastructure/Services`:
  - Tar `HttpClient`, `IOptions<TmdbSettings>`
  - Mappar TMDB-respons (`poster_path` → full URL, `release_date` → år)
  - `genre = "popular"` → `/movie/popular`; annars `/discover/movie?with_genres=<id>` (hardcoded `Dictionary<string, int>`)
- [ ] Registrera i `Infrastructure/DependencyInjection.cs`:
  - `services.Configure<TmdbSettings>(...)`
  - `services.AddHttpClient<ITmdbService, TmdbService>(...)`
- [ ] Tester i `CineMatch.Tests/Services/TmdbServiceTests.cs`:
  - `GetMoviesByGenreAsync_ValidGenre_ReturnsMappedDtos`
  - `GetMoviesByGenreAsync_TmdbReturnsError_ThrowsHttpRequestException`

**Acceptanskriterier**

- [ ] Settings registrerade och validerade vid uppstart (`ApiKey` får inte vara tomt)
- [ ] `HttpClient` registrerad som typed client
- [ ] Service mappar TMDB-respons korrekt
- [ ] Tester gröna utan att slå mot riktig TMDB
- [ ] `README.md` uppdaterad med `dotnet user-secrets set "Tmdb:ApiKey"`-instruktion

---

### S4-06 — Repositories för `Swipe`, `Match` och `WatchPartyMovie`

**Labels:** `backend`, `repository`, `sprint-4`

**Beskrivning**

Tre nya repositories med entity-specifika metoder. Alla ärver från `GenericRepository<T>` och registreras i Infrastructure-DI.

**Tasks**

- [ ] `ISwipeRepository` i `Application/Interfaces/Repositories/`:
  - `GetByPartyMemberAndMovieAsync(Guid partyMemberId, Guid movieId, CancellationToken)`
  - `GetLikesForMovieInPartyAsync(Guid watchPartyId, Guid movieId, CancellationToken)` → returnerar antal likes
  - `GetSwipedMovieIdsForMemberAsync(Guid partyMemberId, CancellationToken)`
- [ ] `IMatchRepository`:
  - `GetByPartyAsync(Guid watchPartyId, CancellationToken)`
  - `ExistsForMovieInPartyAsync(Guid watchPartyId, Guid movieId, CancellationToken)`
- [ ] `IWatchPartyMovieRepository`:
  - `GetMoviesForPartyAsync(Guid watchPartyId, CancellationToken)` → `IReadOnlyList<Movie>` ordnad efter `OrderIndex`
  - `AddRangeAsync(IEnumerable<WatchPartyMovie>, CancellationToken)`
- [ ] Implementationer i `Infrastructure/Database/Repositories/`
- [ ] Registrera alla tre i `Infrastructure/DependencyInjection.cs`

**Acceptanskriterier**

- [ ] Tre interfaces + tre implementationer
- [ ] Alla registrerade i DI
- [ ] Inga DTO-returer från repositories
- [ ] Inga `SaveChangesAsync`-anrop i repositories

**Beroenden:** S4-04

---

### S4-07 — Integrera TMDB i `CreateWatchPartyCommand`

**Labels:** `backend`, `feature`, `watchparty`, `sprint-4`

**Beskrivning**

Uppdatera `CreateWatchPartyCommandHandler` så att den hämtar filmer från TMDB vid party-skapande, dedupar mot befintliga `Movie`-rader via `TmdbId`, och skapar `WatchPartyMovie`-rader med rätt ordning.

> **Notera:** Antalet filmer per party är 50 (`const int MoviesPerParty = 50` i handlern). Flyttas till `TmdbSettings.MoviesPerParty` när det blir konfigurerbart.

**Tasks**

- [ ] Injicera `ITmdbService`, `IMovieRepository`, `IWatchPartyMovieRepository` i `CreateWatchPartyCommandHandler`
- [ ] Efter `watchParty` är skapad men före `SaveChangesAsync`:
  1. Anropa `_tmdbService.GetMoviesByGenreAsync(DefaultGenre, MoviesPerParty, cancellationToken)`
  2. För varje TmdbId, anropa `_movieRepository.GetByTmdbIdAsync(...)`. Om `null` → skapa ny `Movie`
  3. `_movieRepository.BulkInsertAsync(newMovies, cancellationToken)` om listan är icke-tom
  4. Skapa `WatchPartyMovie` per film med stigande `OrderIndex`
  5. `_watchPartyMovieRepository.AddRangeAsync(...)`
- [ ] Logga varning om TMDB returnerar färre filmer än `MoviesPerParty`, fortsätt
- [ ] Uppdatera `CreateWatchPartyCommandHandlerTests`:
  - Mocka `ITmdbService` så att den returnerar 3 dummy-filmer
  - Mocka `IMovieRepository.GetByTmdbIdAsync` så att 2 av 3 redan finns (dedup-test)
  - Verifiera att rätt antal `WatchPartyMovie`-rader skapas med korrekta `OrderIndex`

**Acceptanskriterier**

- [ ] Handlern hämtar filmer från TMDB och skapar `WatchPartyMovie`-rader
- [ ] Dedup mot befintliga `Movie`-rader via `TmdbId` fungerar
- [ ] `OrderIndex` är `0..N-1` i samma ordning TMDB returnerade
- [ ] Alla nya och tidigare tester gröna
- [ ] `SaveChangesAsync` anropas exakt en gång

**Beroenden:** S4-05, S4-06

---

### S4-08 — `MatchDetectionService`

**Labels:** `backend`, `service`, `sprint-4`

**Beskrivning**

Application service som körs efter varje swipe. Avgör om en match ska skapas: om antalet likes för en film i partyt = antalet *aktiva* medlemmar → returnera en ny `Match`-entitet (handlern sparar).

**Tasks**

- [ ] Skapa `IMatchDetectionService` i `Application/Interfaces/Services`:
  - `Task<Match?> DetectMatchAsync(Guid watchPartyId, Guid movieId, CancellationToken)`
- [ ] Implementation `MatchDetectionService` i `Application/Services/` (ny mapp):
  - Injicerar `ISwipeRepository`, `IMatchRepository`, `IPartyMemberRepository`
  - Hämtar antal aktiva medlemmar
  - Hämtar antal likes för filmen
  - Om equal AND `!ExistsForMovieInPartyAsync` → skapa `Match` (returnera, spara inte)
- [ ] Registrera i `Application/DependencyInjection.cs`
- [ ] Tester `MatchDetectionServiceTests`:
  - `DetectMatchAsync_NotAllMembersLiked_ReturnsNull`
  - `DetectMatchAsync_AllMembersLiked_ReturnsMatch`
  - `DetectMatchAsync_MatchAlreadyExists_ReturnsNull`
  - `DetectMatchAsync_IgnoresInactiveMembers_ReturnsMatchIfRemainingAllLiked`

**Acceptanskriterier**

- [ ] Service skapar match endast när alla *aktiva* medlemmar likat
- [ ] Skapar inte dubbletter
- [ ] Sparar inte själv — returnerar till caller
- [ ] Tester gröna

**Beroenden:** S4-06

---

### S4-09 — `CreateSwipeCommand` + handler + validator

**Labels:** `backend`, `feature`, `sprint-4`

**Beskrivning**

Endpoint för att registrera en swipe. Validerar att användaren är aktiv medlem i partyt, sparar swipen, anropar `MatchDetectionService`, returnerar swipe-resultat + eventuell match.

**Tasks**

- [ ] `Features/Swipes/Commands/CreateSwipe/`:
  - `CreateSwipeCommand(Guid WatchPartyId, Guid MovieId, bool IsLiked) : IRequest<ErrorOr<SwipeResultDto>>`
  - `CreateSwipeCommandValidator` (Guids non-empty)
  - `CreateSwipeCommandHandler`:
    - Läs `userId` från `ICurrentUserService` → `SwipeErrors.Unauthorized` om null
    - Hämta aktiv `PartyMember` → `SwipeErrors.NotMemberOfParty` om null
    - Kontrollera att swipe inte redan finns → `SwipeErrors.AlreadySwiped`
    - Kontrollera att filmen är del av partyt (via `WatchPartyMovie`) → `SwipeErrors.MovieNotInParty`
    - Skapa `Swipe`, `_swipeRepository.AddAsync`
    - Om `IsLiked` → `_matchDetectionService.DetectMatchAsync` → om match, `_matchRepository.AddAsync`
    - `_unitOfWork.SaveChangesAsync()` *en gång*
    - Returnera `SwipeResultDto(IsMatch, MatchedMovie)`
- [ ] `SwipeErrors` under `Features/Swipes/Common/Errors/`
- [ ] `SwipeResultDto` under `Features/Swipes/Common/Dtos/`
- [ ] `SwipeMappingProfile` om behövs
- [ ] Tester:
  - `Handle_NotMemberOfParty_ReturnsForbiddenError`
  - `Handle_AlreadySwiped_ReturnsConflictError`
  - `Handle_MovieNotInParty_ReturnsValidationError`
  - `Handle_ValidSwipeNoMatch_ReturnsSwipeResultWithIsMatchFalse`
  - `Handle_ValidSwipeCausesMatch_ReturnsSwipeResultWithMatchedMovie`
  - Verifiera att `UnitOfWork.SaveChangesAsync` anropas exakt en gång

**Acceptanskriterier**

- [ ] Command, handler, validator, errors skapade
- [ ] Alla felscenarier returnerar rätt `ErrorOr`-typ
- [ ] Sparar både `Swipe` och eventuell `Match` i samma transaktion
- [ ] Tester gröna

**Beroenden:** S4-06, S4-08

---

### S4-10 — `GetSwipeQueueQuery` + handler

**Labels:** `backend`, `feature`, `sprint-4`

**Beskrivning**

Hämta nästa N filmer för aktuell user i ett party. Filtrerar bort filmer användaren redan swipat på, returnerar i `OrderIndex`-ordning.

**Tasks**

- [ ] `Features/Swipes/Queries/GetSwipeQueue/`:
  - `GetSwipeQueueQuery(Guid WatchPartyId, int Count = 10) : IRequest<ErrorOr<IReadOnlyList<MovieDto>>>`
  - Handler:
    - Läs `userId` från `ICurrentUserService`
    - Hämta aktiv `PartyMember` → `SwipeErrors.NotMemberOfParty` om null
    - `swipedMovieIds = _swipeRepository.GetSwipedMovieIdsForMemberAsync(partyMemberId)`
    - `partyMovies = _watchPartyMovieRepository.GetMoviesForPartyAsync(watchPartyId)`
    - Filtrera bort swipade, ta `Count` första, map till `MovieDto`
- [ ] Tester:
  - `Handle_NotMember_ReturnsForbidden`
  - `Handle_NoMoviesSwiped_ReturnsFirstNInOrder`
  - `Handle_SomeMoviesSwiped_ReturnsRemainingInOrder`
  - `Handle_AllMoviesSwiped_ReturnsEmptyList`

**Acceptanskriterier**

- [ ] Returnerar filmer i `OrderIndex`-ordning
- [ ] Exkluderar filmer användaren redan swipat på
- [ ] Tester gröna

**Beroenden:** S4-06

---

### S4-11 — `GetMatchesByPartyQuery` + `MarkMatchAsWatchedCommand`

**Labels:** `backend`, `feature`, `sprint-4`

**Beskrivning**

Två enkla operationer paketerade i samma issue eftersom de delar `MatchErrors` och `MatchMappingProfile`.

**Tasks**

- [ ] `Features/Matches/Queries/GetMatchesByParty/`:
  - `GetMatchesByPartyQuery(Guid WatchPartyId) : IRequest<ErrorOr<IReadOnlyList<MatchDto>>>`
  - Handler validerar membership (samma mönster som S4-10)
  - Returnerar matches sorterade på `MatchedAt` desc
- [ ] `Features/Matches/Commands/MarkMatchAsWatched/`:
  - `MarkMatchAsWatchedCommand(Guid MatchId) : IRequest<ErrorOr<MatchDto>>`
  - Handler hämtar match, validerar att user är aktiv medlem i partyt
  - Sätter `IsWatched=true`, `WatchedByUserId=userId`, `WatchedAt=UtcNow`
  - `MatchErrors.NotFound`, `MatchErrors.AlreadyWatched`
- [ ] `MatchDto` under `Features/Matches/Common/Dtos/`
- [ ] `MatchErrors` under `Features/Matches/Common/Errors/`
- [ ] `MatchMappingProfile`
- [ ] Tester:
  - `GetMatchesByParty: Handle_NotMember_ReturnsForbidden / Handle_Valid_ReturnsMatches`
  - `MarkMatchAsWatched: Handle_NotFound / Handle_AlreadyWatched / Handle_Valid_UpdatesFields`

**Acceptanskriterier**

- [ ] Båda flödena skapade
- [ ] DTO + errors + mapping per feature
- [ ] Tester gröna

**Beroenden:** S4-06

---

### S4-12 — `SwipesController` + `MatchesController`

**Labels:** `backend`, `api`, `sprint-4`

**Beskrivning**

Två tunna controllers som dispatchar via MediatR och returnerar via `ToActionResult(this)`. Följer befintlig stil (`WatchPartiesController`).

**Tasks**

- [ ] `SwipesController` i `src/CineMatch.API/Controllers`:
  - `[HttpPost]` → `CreateSwipeCommand` → returnerar `SwipeResultDto`
  - `[HttpGet("queue/{watchPartyId:guid}")]` → `GetSwipeQueueQuery` → `IReadOnlyList<MovieDto>`
  - `[Authorize]` på klassnivå
  - XML-doc-kommentarer för Scalar
  - `[ProducesResponseType]` för 200/400/401/403/404/409
- [ ] `MatchesController`:
  - `[HttpGet("party/{watchPartyId:guid}")]` → `GetMatchesByPartyQuery`
  - `[HttpPost("{matchId:guid}/watched")]` → `MarkMatchAsWatchedCommand`
- [ ] Verifiera i Scalar (`/scalar`)

**Acceptanskriterier**

- [ ] Båda controllers skapade
- [ ] Alla endpoints kräver `[Authorize]`
- [ ] Endpoints synliga i Scalar
- [ ] Inga handlers/repositories anropade direkt — bara via MediatR

**Beroenden:** S4-09, S4-10, S4-11

---

### S4-13 — Uppdatera `CLAUDE.md`, UML och userFlow för Sprint 4

**Labels:** `backend`, `docs`, `sprint-4`

**Beskrivning**

Synka dokumentation med koden efter Sprint 4 är klar.

**Tasks**

- [ ] **`CLAUDE.md`:**
  - Lägg till `WatchPartyMovie` i Domain Model-sektionen
  - Lägg till `WatchedByUserId` och `WatchedAt` på `Match`
  - Dokumentera TMDB-integration (user-secrets för `Tmdb:ApiKey`, `MoviesPerParty = 50`)
  - Dokumentera `MatchDetectionService` (`Application/Services/`)
  - Dokumentera "alla aktiva medlemmar"-regeln för match-detektering
  - Ta bort "TMDB-integration kommer i sprint 4" från Open Questions
- [ ] **`docs/uml-class-diagram.md`:**
  - Lägg till `WatchPartyMovie`-klass
  - Lägg till relationer: `WatchParty 1..* WatchPartyMovie *..1 Movie`
  - Note: `Unique: (WatchPartyId, MovieId), (WatchPartyId, OrderIndex)`
- [ ] **`docs/userFlow-diagram.md`:** Förtydliga "Hämta filmer" → "Hämta filmer från party (cachat vid skapande)"
- [ ] Uppdatera `docs/plan.md` Sprint 4-tabellen (alla issues `Done`)

**Acceptanskriterier**

- [ ] `CLAUDE.md` uppdaterad
- [ ] UML-diagrammet renderar korrekt i GitHub (Mermaid)
- [ ] userFlow-diagrammet uppdaterat
- [ ] Inget nämner längre att Sprint 4 är "kommande"

**Beroenden:** S4-12 (görs sist, efter all kod är mergad)
