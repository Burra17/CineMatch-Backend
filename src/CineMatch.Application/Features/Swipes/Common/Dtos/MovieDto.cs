namespace CineMatch.Application.Features.Swipes.Common.Dtos;

public record MovieDto(
    Guid Id,
    int TmdbId,
    string Title,
    string PosterUrl,
    string Overview,
    int ReleaseYear);
