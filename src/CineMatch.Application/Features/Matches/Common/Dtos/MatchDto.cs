namespace CineMatch.Application.Features.Matches.Common.Dtos;

public record MatchDto(
    Guid Id,
    Guid WatchPartyId,
    Guid MovieId,
    DateTime MatchedAt,
    bool IsWatched,
    Guid? WatchedByUserId,
    DateTime? WatchedAt);
