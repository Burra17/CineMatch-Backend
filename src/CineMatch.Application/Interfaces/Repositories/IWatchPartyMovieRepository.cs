using CineMatch.Domain.Models;

namespace CineMatch.Application.Interfaces.Repositories;

public interface IWatchPartyMovieRepository : IGenericRepository<WatchPartyMovie>
{
    Task<IReadOnlyList<Movie>> GetMoviesForPartyAsync(Guid watchPartyId, CancellationToken cancellationToken);
    Task AddRangeAsync(IEnumerable<WatchPartyMovie> watchPartyMovies, CancellationToken cancellationToken);
}
