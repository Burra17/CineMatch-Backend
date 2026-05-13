using CineMatch.Domain.Models;

namespace CineMatch.Application.Interfaces.Repositories;

public interface IMovieRepository : IGenericRepository<Movie>
{
    Task<Movie?> GetByTmdbIdAsync(int tmdbId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Movie>> GetExistingByTmdbIdsAsync(IEnumerable<int> tmdbIds, CancellationToken cancellationToken);
    Task BulkInsertAsync(IEnumerable<Movie> movies, CancellationToken cancellationToken);
}
