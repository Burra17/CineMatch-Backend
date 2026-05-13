using CineMatch.Application.Interfaces.Repositories;
using CineMatch.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace CineMatch.Infrastructure.Database.Repositories;

public class MatchRepository : GenericRepository<Match>, IMatchRepository
{
    public MatchRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Match>> GetByPartyAsync(Guid watchPartyId, CancellationToken cancellationToken)
    {
        return await _context.Matches
            .Where(m => m.WatchPartyId == watchPartyId)
            .OrderByDescending(m => m.MatchedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsForMovieInPartyAsync(Guid watchPartyId, Guid movieId, CancellationToken cancellationToken)
    {
        return await _context.Matches
            .AnyAsync(m => m.WatchPartyId == watchPartyId && m.MovieId == movieId, cancellationToken);
    }
}
