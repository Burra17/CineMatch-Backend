using CineMatch.Application.Interfaces.Repositories;
using CineMatch.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace CineMatch.Infrastructure.Database.Repositories;

public class WatchPartyMovieRepository : GenericRepository<WatchPartyMovie>, IWatchPartyMovieRepository
{
    public WatchPartyMovieRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Movie>> GetMoviesForPartyAsync(Guid watchPartyId, CancellationToken cancellationToken)
    {
        return await _context.WatchPartyMovies
            .Where(wpm => wpm.WatchPartyId == watchPartyId)
            .OrderBy(wpm => wpm.OrderIndex)
            .Select(wpm => wpm.Movie)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsInPartyAsync(Guid watchPartyId, Guid movieId, CancellationToken cancellationToken)
    {
        return await _context.WatchPartyMovies
            .AnyAsync(wpm => wpm.WatchPartyId == watchPartyId && wpm.MovieId == movieId, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<WatchPartyMovie> watchPartyMovies, CancellationToken cancellationToken)
    {
        await _context.WatchPartyMovies.AddRangeAsync(watchPartyMovies, cancellationToken);
    }
}
