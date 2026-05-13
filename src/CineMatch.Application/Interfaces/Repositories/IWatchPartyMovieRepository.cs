using CineMatch.Domain.Models;

namespace CineMatch.Application.Interfaces.Repositories;

public interface IWatchPartyMovieRepository : IGenericRepository<WatchPartyMovie>
{
    Task<IReadOnlyList<Movie>> GetMoviesForPartyAsync(Guid watchPartyId, CancellationToken cancellationToken);
    Task<bool> ExistsInPartyAsync(Guid watchPartyId, Guid movieId, CancellationToken cancellationToken);
    Task AddRangeAsync(IEnumerable<WatchPartyMovie> watchPartyMovies, CancellationToken cancellationToken);
}
