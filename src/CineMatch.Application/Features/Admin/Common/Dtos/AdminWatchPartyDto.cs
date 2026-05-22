namespace CineMatch.Application.Features.Admin.Common.Dtos;

public record AdminWatchPartyDto(
    Guid Id,
    string JoinCode,
    string HostUsername,
    int MemberCount,
    bool IsActive,
    DateTime CreatedAt
    );
