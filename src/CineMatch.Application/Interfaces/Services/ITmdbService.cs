using CineMatch.Application.Features.Movies.Common.Dtos;

namespace CineMatch.Application.Interfaces.Services;

public interface ITmdbService
{
    Task<IReadOnlyList<TmdbMovieDto>> GetMoviesByGenreAsync(string genre, int count, CancellationToken cancellationToken);
}
