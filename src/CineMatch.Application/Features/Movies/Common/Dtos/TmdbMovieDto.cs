namespace CineMatch.Application.Features.Movies.Common.Dtos;

public record TmdbMovieDto(
    int TmdbId,
    string Title,
    string PosterUrl,
    string Overview,
    int ReleaseYear);
