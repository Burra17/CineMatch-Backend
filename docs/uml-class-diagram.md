```mermaid
classDiagram
    direction TB

    %% Enum
    class UserRole {
        <<enumeration>>
        User
        Admin
    }

    %% Classes
    class User {
        +Guid Id
        +string Username
        +string Email
        +string PasswordHash
        +UserRole Role
        +bool IsEmailConfirmed
        +DateTime CreatedAt
        +DateTime? UpdatedAt
    }

    class WatchParty {
        +Guid Id
        +string JoinCode
        +Guid HostId
        +string Genre
        +bool IsActive
        +DateTime CreatedAt
        +DateTime? ClosedAt
    }

    class PartyMember {
        +Guid Id
        +Guid UserId
        +Guid WatchPartyId
        +DateTime JoinedAt
        +DateTime? LeftAt
        +bool IsActive
    }

    class Movie {
        +Guid Id
        +int TmdbId
        +string Title
        +string PosterUrl
        +string Overview
        +int ReleaseYear
        +DateTime CachedAt
    }

    class WatchPartyMovie {
        +Guid Id
        +Guid WatchPartyId
        +Guid MovieId
        +int OrderIndex
        +DateTime AddedAt
    }

    class Swipe {
        +Guid Id
        +Guid PartyMemberId
        +Guid WatchPartyId
        +Guid MovieId
        +bool IsLiked
        +DateTime SwipedAt
    }

    class Match {
        +Guid Id
        +Guid WatchPartyId
        +Guid MovieId
        +DateTime MatchedAt
        +bool IsWatched
        +Guid? WatchedByUserId
        +DateTime? WatchedAt
    }

    class PasswordResetToken {
        +Guid Id
        +Guid UserId
        +string TokenHash
        +DateTime ExpiresAt
        +bool IsUsed
        +DateTime CreatedAt
        +DateTime? UsedAt
    }

    class RefreshToken {
        +Guid Id
        +Guid UserId
        +string TokenHash
        +DateTime ExpiresAt
        +bool IsRevoked
        +DateTime CreatedAt
        +DateTime? RevokedAt
    }

    %% Relations
    User "1" --> "*" WatchParty : Hosts
    User "1" --> "*" PartyMember : Has
    WatchParty "1" --> "*" PartyMember : Contains
    User "1" --> "*" PasswordResetToken : Owns
    User "1" --> "*" RefreshToken : Owns
    User "0..1" --> "*" Match : MarksAsWatched

    WatchParty "1" --> "*" WatchPartyMovie : Includes
    Movie "1" --> "*" WatchPartyMovie : ListedIn

    WatchParty "1" --> "*" Swipe : Has
    PartyMember "1" --> "*" Swipe : Makes
    Movie "1" --> "*" Swipe : TargetOf

    WatchParty "1" --> "*" Match : ResultsIn
    Movie "1" --> "*" Match : MatchedMovie

    %% Unique Constraints Notes
    note for User "Unique Constraints:\nEmail\nUsername"
    note for Movie "Unique Constraint:\nTmdbId"
    note for WatchParty "Unique Constraint:\nJoinCode (bland aktiva partyn)"
    note for WatchPartyMovie "Composite Unique Constraints:\n(WatchPartyId, MovieId)\n(WatchPartyId, OrderIndex)"
    note for Swipe "Composite Unique Constraint:\n(PartyMemberId, MovieId)"
    note for PasswordResetToken "Unique Constraint:\nTokenHash"
    note for RefreshToken "Unique Constraint:\nTokenHash"
   ```