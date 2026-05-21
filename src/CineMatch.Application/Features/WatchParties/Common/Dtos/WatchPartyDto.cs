namespace CineMatch.Application.Features.WatchParties.Common.Dtos;

public record WatchPartyDto(
    Guid Id,
    string JoinCode,
    string HostUsername,
    string Genre,
    bool IsActive,
    bool IsStarted,
    DateTime CreatedAt,
    int MemberCount
);
