# CineMatch API — Kontrakt

Komplett dokumentation av alla API-endpoints, request/response-format, valideringsregler och felkoder.

---

## Generellt

### Bas-URL
```
https://<host>/api
```

### Autentisering
Skyddade endpoints kräver en JWT-token i `Authorization`-headern:
```
Authorization: Bearer <token>
```
Token erhålls via `POST /api/auth/login` och är giltig i **1 timme**.

### Content-Type
Alla request bodies skickas som `application/json`.

---

## Felformat

### Applikationsfel (valideringsfel, domänfel)
Returneras av alla endpoints som ett JSON-array med ett eller flera felobjekt. HTTP-statuskoden bestäms av det **första** felets typ.

```json
[
  {
    "code": "User.EmailAlreadyExists",
    "description": "An account with this email already exists",
    "type": 4,
    "numericType": 4
  }
]
```

| Fält          | Typ    | Beskrivning                            |
|---------------|--------|----------------------------------------|
| `code`        | string | Maskinläsbar felkod (`Entity.Reason`)  |
| `description` | string | Mänsklig felbeskrivning                |
| `type`        | int    | ErrorType-enum (se tabell nedan)       |
| `numericType` | int    | Samma värde som `type`                 |

**ErrorType-enum:**

| Värde | Namn         | HTTP-status |
|-------|--------------|-------------|
| 0     | Failure      | 500         |
| 1     | Unexpected   | 500         |
| 2     | Validation   | 400         |
| 3     | NotFound     | 404         |
| 4     | Conflict     | 409         |
| 5     | Unauthorized | 401         |
| 6     | Forbidden    | 403         |

### Ohantererade undantag (500)
Returneras av `ExceptionHandlingMiddleware` för oväntade fel:

```json
{
  "status": 500,
  "title": "An error occurred while processing your request",
  "traceId": "00-abc123...",
  "detail": "Optional exception detail"
}
```

---

## Statuskodöversikt

| HTTP-status | När                                     |
|-------------|-----------------------------------------|
| 200 OK      | Lyckad operation med responsdata        |
| 204 No Content | Lyckad operation utan responsdata    |
| 400 Bad Request | Valideringsfel (FluentValidation eller domän) |
| 401 Unauthorized | Saknad/ogiltig JWT eller felaktiga inloggningsuppgifter |
| 403 Forbidden | Autentiserad men saknar behörighet    |
| 404 Not Found | Resursen hittades inte               |
| 409 Conflict | Resurskonflikt (dubletter etc.)        |
| 500 Internal Server Error | Oväntat serverfel          |

---

## Gemensamma DTO-typer

### `UserDto`
```json
{
  "id": "uuid",
  "username": "string",
  "email": "string",
  "role": 0,
  "createdAt": "datetime (UTC)"
}
```
> `role`: `0` = User, `1` = Admin

### `MovieDto`
```json
{
  "id": "uuid",
  "tmdbId": 12345,
  "title": "string",
  "posterUrl": "string",
  "overview": "string",
  "releaseYear": 2024
}
```

### `WatchPartyDto`
```json
{
  "id": "uuid",
  "joinCode": "ABC123",
  "hostUsername": "string",
  "genre": "string",
  "isActive": true,
  "createdAt": "datetime (UTC)",
  "memberCount": 1
}
```

### `WatchPartyDetailsDto`
```json
{
  "id": "uuid",
  "joinCode": "ABC123",
  "hostUsername": "string",
  "genre": "string",
  "isActive": true,
  "createdAt": "datetime (UTC)",
  "memberCount": 2,
  "members": [
    {
      "userId": "uuid",
      "username": "string",
      "joinedAt": "datetime (UTC)",
      "isActive": true
    }
  ]
}
```
> `members` innehåller **alla** medlemmar, även inaktiva (de som lämnat partyt). Kontrollera `isActive` för att filtrera.

### `SwipeResultDto`
```json
{
  "isMatch": false,
  "matchedMovie": null
}
```
> `matchedMovie` är ett `MovieDto`-objekt om `isMatch` är `true`, annars `null`.

### `MatchDto`
```json
{
  "id": "uuid",
  "watchPartyId": "uuid",
  "movieId": "uuid",
  "matchedAt": "datetime (UTC)",
  "isWatched": false,
  "watchedByUserId": null,
  "watchedAt": null
}
```

---

## Auth-endpoints

Bas-route: `/api/auth`  
Alla Auth-endpoints är **publika** (kräver ej JWT).

---

### `POST /api/auth/register`

Registrerar ett nytt användarkonto.

**Request body:**
```json
{
  "username": "string",
  "email": "string",
  "password": "string"
}
```

**Valideringsregler:**
| Fält       | Regler                                              |
|------------|-----------------------------------------------------|
| `username` | Obligatorisk, min 3 tecken, max 20 tecken           |
| `email`    | Obligatorisk, måste vara giltig e-postadress        |
| `password` | Obligatorisk, min 6 tecken, max 100 tecken          |

**Svar: `200 OK`** — `UserDto`
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "username": "johndoe",
  "email": "john@example.com",
  "role": 0,
  "createdAt": "2025-01-15T10:00:00Z"
}
```

**Felsvar:**
| HTTP | Felkod                      | Beskrivning                                   |
|------|-----------------------------|-----------------------------------------------|
| 400  | *(validation)*              | Obligatoriskt fält saknas eller är ogiltigt   |
| 409  | `User.EmailAlreadyExists`   | An account with this email already exists     |
| 409  | `User.UsernameAlreadyExists`| This username is already taken                |

---

### `POST /api/auth/login`

Autentiserar en användare och returnerar en JWT-token.

**Request body:**
```json
{
  "email": "string",
  "password": "string"
}
```

**Valideringsregler:**
| Fält       | Regler                                       |
|------------|----------------------------------------------|
| `email`    | Obligatorisk, måste vara giltig e-postadress |
| `password` | Obligatorisk                                 |

**Svar: `200 OK`** — `LoginResponseDto`
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "user": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "username": "johndoe",
    "email": "john@example.com",
    "role": 0,
    "createdAt": "2025-01-15T10:00:00Z"
  }
}
```

**Felsvar:**
| HTTP | Felkod                   | Beskrivning               |
|------|--------------------------|---------------------------|
| 400  | *(validation)*           | Obligatoriskt fält saknas |
| 401  | `Auth.InvalidCredentials`| Invalid email or password |

> `Auth.InvalidCredentials` returneras vid **både** fel e-post och fel lösenord för att förhindra användaruppräkning.

---

### `POST /api/auth/forgot-password`

Begär en lösenordsåterställningstoken för ett e-postkonto.

**Request body:**
```json
{
  "email": "string"
}
```

**Valideringsregler:**
| Fält    | Regler                                       |
|---------|----------------------------------------------|
| `email` | Obligatorisk, måste vara giltig e-postadress |

**Svar: `200 OK`** — rå återställningstoken (sträng)
```
"SGVsbG8gV29ybGQh..."
```
> I **development** returneras den råa Base64-token direkt i svarets body. I produktion ska den skickas via e-post istället.  
> Token: 32 kryptografiskt slumpmässiga bytes, Base64-kodad. Giltig i **1 timme**, engångsanvändning.

**Felsvar:**
| HTTP | Felkod         | Beskrivning                         |
|------|----------------|-------------------------------------|
| 400  | *(validation)* | Ogiltig e-postadress                |
| 404  | `User.NotFound`| The requested user was not found    |

---

### `POST /api/auth/reset-password`

Återställer lösenordet med hjälp av en giltig återställningstoken.

**Request body:**
```json
{
  "token": "string",
  "newPassword": "string"
}
```

**Valideringsregler:**
| Fält          | Regler                                     |
|---------------|--------------------------------------------|
| `token`       | Obligatorisk                               |
| `newPassword` | Obligatorisk, min 8 tecken, max 100 tecken |

**Svar: `204 No Content`** — tomt svar

**Felsvar:**
| HTTP | Felkod                               | Beskrivning                                                                   |
|------|--------------------------------------|-------------------------------------------------------------------------------|
| 400  | *(validation)*                       | Obligatoriskt fält saknas eller lösenordet uppfyller inte längdkraven         |
| 400  | `PasswordReset.InvalidOrExpiredToken`| The password reset token is invalid, expired, or has already been used        |

> Samma felkod returneras oavsett om token är ogiltig, utgången eller redan använd — ingen distinktion görs till anroparen.

---

## Users-endpoints

Bas-route: `/api/users`  
Alla endpoints kräver **autentisering** (JWT).

---

### `GET /api/users/me`

Returnerar information om den inloggade användaren.

**Request:** Ingen body, inga parametrar.

**Svar: `200 OK`** — `UserDto`
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "username": "johndoe",
  "email": "john@example.com",
  "role": 0,
  "createdAt": "2025-01-15T10:00:00Z"
}
```

**Felsvar:**
| HTTP | Felkod         | Beskrivning                                                   |
|------|----------------|---------------------------------------------------------------|
| 401  | *(JWT-fel)*    | Saknad eller ogiltig JWT-token                                |
| 404  | `User.NotFound`| The requested user was not found (token giltig, men kontot borttaget) |

---

## WatchParties-endpoints

Bas-route: `/api/watchparties`  
Alla endpoints kräver **autentisering** (JWT).

---

### `POST /api/watchparties`

Skapar ett nytt WatchParty. Anroparen blir host och första aktiv medlem. Hämtar 50 filmer från TMDB och skapar swipe-kön.

**Request:** Ingen body.

**Svar: `200 OK`** — `WatchPartyDto`
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "joinCode": "XK7M2P",
  "hostUsername": "johndoe",
  "genre": "popular",
  "isActive": true,
  "createdAt": "2025-01-15T10:00:00Z",
  "memberCount": 1
}
```

> `joinCode`: 6 tecken, kryptografisk slumpmässighet, inget O/0, I/1, l för att undvika förväxling.  
> `genre` är för tillfället alltid `"popular"` (fast värde).

**Felsvar:**
| HTTP | Felkod                   | Beskrivning            |
|------|--------------------------|------------------------|
| 401  | `WatchParty.Unauthorized`| The user is unauthorized. |

---

### `POST /api/watchparties/join`

Går med i ett befintligt WatchParty via join-kod. Idempotent: om användaren redan är aktiv medlem returneras partyt utan ändringar. Om användaren tidigare lämnat reaktiveras medlemskapet.

**Request body:**
```json
{
  "joinCode": "string"
}
```

**Valideringsregler:**
| Fält       | Regler                         |
|------------|--------------------------------|
| `joinCode` | Obligatorisk, exakt 6 tecken   |

**Svar: `200 OK`** — `WatchPartyDto`
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "joinCode": "XK7M2P",
  "hostUsername": "johndoe",
  "genre": "popular",
  "isActive": true,
  "createdAt": "2025-01-15T10:00:00Z",
  "memberCount": 2
}
```

**Felsvar:**
| HTTP | Felkod                       | Beskrivning                                 |
|------|------------------------------|---------------------------------------------|
| 400  | *(validation)*               | Join-koden saknas eller har fel längd       |
| 401  | `WatchParty.Unauthorized`    | The user is unauthorized.                   |
| 404  | `WatchParty.JoinCodeNotFound`| The requested join code was not found.      |

---

### `POST /api/watchparties/{id}/leave`

Lämnar ett WatchParty. Mjukradering av medlemskapet (`IsActive = false`, `LeftAt` stämplas). Om användaren är sista aktiva medlem stängs partyt (`IsActive = false`, `ClosedAt` stämplas).

**Route-parametrar:**
| Parameter | Typ  | Beskrivning    |
|-----------|------|----------------|
| `id`      | guid | WatchParty-ID  |

**Request:** Ingen body.

**Svar: `200 OK`** — tomt objekt `{}`

**Felsvar:**
| HTTP | Felkod                    | Beskrivning                                                      |
|------|---------------------------|------------------------------------------------------------------|
| 401  | `WatchParty.Unauthorized` | The user is unauthorized.                                        |
| 403  | `WatchParty.UserNotMember`| The user is not a member of the requested WatchParty.            |
| 404  | `WatchParty.NotFound`     | The requested WatchParty was not found.                          |
| 409  | `WatchParty.HostCannotLeave` | The host is not allowed to leave the party. (När fler aktiva medlemmar finns) |

> **Hostlogik:** Host kan bara lämna om hen är den enda aktiva medlemmen. Hostöverföring är ej implementerat.

---

### `GET /api/watchparties/{id}`

Hämtar detaljer för ett WatchParty, inklusive fullständig medlemslista. Enbart aktiva medlemmar kan komma åt endpointen.

**Route-parametrar:**
| Parameter | Typ  | Beskrivning    |
|-----------|------|----------------|
| `id`      | guid | WatchParty-ID  |

**Request:** Ingen body.

**Svar: `200 OK`** — `WatchPartyDetailsDto`
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "joinCode": "XK7M2P",
  "hostUsername": "johndoe",
  "genre": "popular",
  "isActive": true,
  "createdAt": "2025-01-15T10:00:00Z",
  "memberCount": 2,
  "members": [
    {
      "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "username": "johndoe",
      "joinedAt": "2025-01-15T10:00:00Z",
      "isActive": true
    },
    {
      "userId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
      "username": "janedoe",
      "joinedAt": "2025-01-15T10:05:00Z",
      "isActive": false
    }
  ]
}
```

> `members` inkluderar **alla** som någonsin gick med, även inaktiva (lämnade). `isActive = false` innebär att de lämnat.

**Felsvar:**
| HTTP | Felkod                    | Beskrivning                                            |
|------|---------------------------|--------------------------------------------------------|
| 401  | `WatchParty.Unauthorized` | The user is unauthorized.                              |
| 403  | `WatchParty.UserNotMember`| The user is not a member of the requested WatchParty.  |
| 404  | `WatchParty.NotFound`     | The requested WatchParty was not found.                |

---

## Swipes-endpoints

Bas-route: `/api/swipes`  
Alla endpoints kräver **autentisering** (JWT).

---

### `POST /api/swipes`

Registrerar ett svep (gilla eller ogilla) på en film i ett WatchParty. Returnerar om ett match skapades.

**Request body:**
```json
{
  "watchPartyId": "uuid",
  "movieId": "uuid",
  "isLiked": true
}
```

**Valideringsregler:**
| Fält           | Regler      |
|----------------|-------------|
| `watchPartyId` | Obligatorisk (ej tom GUID) |
| `movieId`      | Obligatorisk (ej tom GUID) |

**Svar: `200 OK`** — `SwipeResultDto`

*Inget match:*
```json
{
  "isMatch": false,
  "matchedMovie": null
}
```

*Match skapad:*
```json
{
  "isMatch": true,
  "matchedMovie": {
    "id": "uuid",
    "tmdbId": 12345,
    "title": "Inception",
    "posterUrl": "https://image.tmdb.org/...",
    "overview": "A thief who steals...",
    "releaseYear": 2010
  }
}
```

> Match skapas när **alla aktiva medlemmar** i partyt har gillat samma film. Match-detektion körs bara om `isLiked` är `true`.

**Felsvar:**
| HTTP | Felkod                   | Beskrivning                                             |
|------|--------------------------|---------------------------------------------------------|
| 400  | *(validation)*           | `watchPartyId` eller `movieId` är tom GUID / saknas     |
| 400  | `Swipe.MovieNotInParty`  | The movie is not part of this party's queue.            |
| 401  | `Swipe.Unauthorized`     | The user is unauthorized.                               |
| 403  | `Swipe.NotMemberOfParty` | The user is not an active member of this party.         |
| 409  | `Swipe.AlreadySwiped`    | The user has already swiped on this movie in this party.|

---

### `GET /api/swipes/queue/{watchPartyId}`

Hämtar nästa filmer att svepa på för den inloggade användaren i ett WatchParty. Exkluderar filmer som redan svepats på. Returnerar filmer i kö-ordning.

**Route-parametrar:**
| Parameter      | Typ  | Beskrivning    |
|----------------|------|----------------|
| `watchPartyId` | guid | WatchParty-ID  |

**Query-parametrar:**
| Parameter | Typ | Standard | Beskrivning                          |
|-----------|-----|----------|--------------------------------------|
| `count`   | int | `10`     | Antal filmer att returnera (max ~50) |

**Svar: `200 OK`** — array av `MovieDto`
```json
[
  {
    "id": "uuid",
    "tmdbId": 12345,
    "title": "Inception",
    "posterUrl": "https://image.tmdb.org/...",
    "overview": "A thief who steals...",
    "releaseYear": 2010
  }
]
```
> Tom array `[]` om inga filmer återstår att svepa på.

**Felsvar:**
| HTTP | Felkod                   | Beskrivning                                     |
|------|--------------------------|-------------------------------------------------|
| 401  | `Swipe.Unauthorized`     | The user is unauthorized.                       |
| 403  | `Swipe.NotMemberOfParty` | The user is not an active member of this party. |

---

## Matches-endpoints

Bas-route: `/api/matches`  
Alla endpoints kräver **autentisering** (JWT).

---

### `GET /api/matches/party/{watchPartyId}`

Hämtar alla matcher för ett WatchParty, sorterade med nyast först. Enbart aktiva medlemmar kan komma åt endpointen.

**Route-parametrar:**
| Parameter      | Typ  | Beskrivning    |
|----------------|------|----------------|
| `watchPartyId` | guid | WatchParty-ID  |

**Request:** Ingen body.

**Svar: `200 OK`** — array av `MatchDto`
```json
[
  {
    "id": "uuid",
    "watchPartyId": "uuid",
    "movieId": "uuid",
    "matchedAt": "2025-01-15T11:00:00Z",
    "isWatched": false,
    "watchedByUserId": null,
    "watchedAt": null
  }
]
```
> Tom array `[]` om inga matcher finns ännu.

**Felsvar:**
| HTTP | Felkod                    | Beskrivning                                     |
|------|---------------------------|-------------------------------------------------|
| 401  | `Match.Unauthorized`      | The user is unauthorized.                       |
| 403  | `Match.NotMemberOfParty`  | The user is not an active member of this party. |

---

### `POST /api/matches/{matchId}/watched`

Markerar en matchad film som sedd. Kan enbart utföras av aktiva medlemmar i partyt som äger matchen.

**Route-parametrar:**
| Parameter | Typ  | Beskrivning |
|-----------|------|-------------|
| `matchId` | guid | Match-ID    |

**Request:** Ingen body.

**Svar: `200 OK`** — `MatchDto`
```json
{
  "id": "uuid",
  "watchPartyId": "uuid",
  "movieId": "uuid",
  "matchedAt": "2025-01-15T11:00:00Z",
  "isWatched": true,
  "watchedByUserId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "watchedAt": "2025-01-15T20:00:00Z"
}
```

**Felsvar:**
| HTTP | Felkod                   | Beskrivning                                     |
|------|--------------------------|-------------------------------------------------|
| 401  | `Match.Unauthorized`     | The user is unauthorized.                       |
| 403  | `Match.NotMemberOfParty` | The user is not an active member of this party. |
| 404  | `Match.NotFound`         | The requested match was not found.              |
| 409  | `Match.AlreadyWatched`   | This match has already been marked as watched.  |

---

## Felkodsregister

Komplett lista över alla felkoder som kan returneras av API:t.

| Felkod                               | ErrorType    | HTTP | Beskrivning                                                                 |
|--------------------------------------|--------------|------|-----------------------------------------------------------------------------|
| `Auth.InvalidCredentials`            | Unauthorized | 401  | Invalid email or password                                                   |
| `User.EmailAlreadyExists`            | Conflict     | 409  | An account with this email already exists                                   |
| `User.UsernameAlreadyExists`         | Conflict     | 409  | This username is already taken                                              |
| `User.NotFound`                      | NotFound     | 404  | The requested user was not found                                            |
| `PasswordReset.InvalidOrExpiredToken`| Validation   | 400  | The password reset token is invalid, expired, or has already been used      |
| `WatchParty.NotFound`                | NotFound     | 404  | The requested WatchParty was not found.                                     |
| `WatchParty.JoinCodeNotFound`        | NotFound     | 404  | The requested join code was not found.                                      |
| `WatchParty.UserNotMember`           | Forbidden    | 403  | The user is not a member of the requested WatchParty.                       |
| `WatchParty.HostCannotLeave`         | Conflict     | 409  | The host is not allowed to leave the party.                                 |
| `WatchParty.Unauthorized`            | Unauthorized | 401  | The user is unauthorized.                                                   |
| `Swipe.Unauthorized`                 | Unauthorized | 401  | The user is unauthorized.                                                   |
| `Swipe.NotMemberOfParty`             | Forbidden    | 403  | The user is not an active member of this party.                             |
| `Swipe.AlreadySwiped`                | Conflict     | 409  | The user has already swiped on this movie in this party.                    |
| `Swipe.MovieNotInParty`              | Validation   | 400  | The movie is not part of this party's queue.                                |
| `Match.Unauthorized`                 | Unauthorized | 401  | The user is unauthorized.                                                   |
| `Match.NotMemberOfParty`             | Forbidden    | 403  | The user is not an active member of this party.                             |
| `Match.NotFound`                     | NotFound     | 404  | The requested match was not found.                                          |
| `Match.AlreadyWatched`               | Conflict     | 409  | This match has already been marked as watched.                              |

---

## Endpoints — snabböversikt

| Metod  | Route                                  | Auth       | Beskrivning                              |
|--------|----------------------------------------|------------|------------------------------------------|
| POST   | `/api/auth/register`                   | Publik     | Registrera ny användare                  |
| POST   | `/api/auth/login`                      | Publik     | Logga in, hämta JWT                      |
| POST   | `/api/auth/forgot-password`            | Publik     | Begär lösenordsåterställningstoken       |
| POST   | `/api/auth/reset-password`             | Publik     | Återställ lösenord med token             |
| GET    | `/api/users/me`                        | JWT        | Hämta inloggad användare                 |
| POST   | `/api/watchparties`                    | JWT        | Skapa WatchParty                         |
| POST   | `/api/watchparties/join`               | JWT        | Gå med i WatchParty via join-kod         |
| POST   | `/api/watchparties/{id}/leave`         | JWT        | Lämna WatchParty                         |
| GET    | `/api/watchparties/{id}`               | JWT        | Hämta WatchParty-detaljer                |
| POST   | `/api/swipes`                          | JWT        | Registrera svep (gilla/ogilla)           |
| GET    | `/api/swipes/queue/{watchPartyId}`     | JWT        | Hämta filmkö att svepa på               |
| GET    | `/api/matches/party/{watchPartyId}`    | JWT        | Hämta alla matcher för WatchParty        |
| POST   | `/api/matches/{matchId}/watched`       | JWT        | Markera match som sedd                   |
