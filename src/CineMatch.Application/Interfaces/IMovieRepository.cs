using CineMatch.Domain.Models;

namespace CineMatch.Application.Interfaces
{
    public interface IMovieRepository : IGenericRepository<Movie>
    {
        Task<Movie?> GetByTmdbIdAsync(int tmdbId, CancellationToken cancellationToken);
        Task BulkInsertAsync(IEnumerable<Movie> movies, CancellationToken cancellationToken);
    }
}
