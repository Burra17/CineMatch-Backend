```mermaid
flowchart TD
    A[Öppna appen] --> B{Inloggad?}
    B -- Nej --> C[Landningssida]
    C --> D[Register]
    C --> E[Login]
    C --> F[Reset Password]
    D --> G[Skapa konto]
    G --> H[Dashboard]
    E --> I{Lyckad inloggning?}
    I -- Ja --> H
    I -- Nej --> J[Felmeddelande]
    J --> E
    F --> K[Skicka återställning]
    K --> C
    B -- Ja --> H

    H --> L{Skapa eller gå med?}
    L -- Skapa party --> M[Välj genre]
    M --> N[Generera JoinCode]
    N --> O[Dela JoinCode]
    O --> P[Lobby]

    L -- Gå med i party --> Q[Ange JoinCode]
    Q --> R{JoinCode giltig?}
    R -- Ja --> P
    R -- Nej --> S[Fel: ogiltig kod]
    S --> Q

    P --> T{Host startar?}
    T -- Nej --> U[Vänta i lobby]
    U --> T
    T -- Ja --> V[Starta swipe-session]

    V --> W[Hämta filmer från party (cachat vid skapande)]
    W --> X{Filmer tillgängliga?}
    X -- Nej --> Y[Fel: inga fler filmer]
    X -- Ja --> Z[Visa filmkort]

    Z --> AA[Swipe like / dislike]
    AA --> AB[Skicka swipe till backend]
    AB --> AC{Nätverksfel?}
    AC -- Ja --> AD[Fel mot backend / TMDB]
    AD --> W
    AC -- Nej --> AE{Match uppnådd?}
    AE -- Nej --> Z
    AE -- Ja --> AF[Notifiera alla i partyt]

    AF --> AG[Match-vy]
    AG --> AH[Visa poster, beskrivning, år]
    AH --> AI[Markera som sedd]
    AI --> AJ{Fortsätt eller avsluta?}
    AJ -- Fortsätt --> Z
    AJ -- Avsluta --> AK[Avsluta session]
```
