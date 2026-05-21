namespace CineMatch.Application.Features.Matches.Common.Dtos;

public record MatchDto(
    Guid Id,
    Guid WatchPartyId,
    Guid MovieId,
    string? MovieTitle,
    string? MoviePosterUrl,
    string? MovieOverview,
    int? MovieReleaseYear,
    DateTime MatchedAt,
    bool IsWatched,
    Guid? WatchedByUserId,
    DateTime? WatchedAt);
