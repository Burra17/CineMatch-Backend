using CineMatch.Application.Interfaces.Repositories;
using CineMatch.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace CineMatch.Infrastructure.Database.Repositories;

public class SwipeRepository : GenericRepository<Swipe>, ISwipeRepository
{
    public SwipeRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Swipe?> GetByPartyMemberAndMovieAsync(Guid partyMemberId, Guid movieId, CancellationToken cancellationToken)
    {
        return await _context.Swipes
            .FirstOrDefaultAsync(s => s.PartyMemberId == partyMemberId && s.MovieId == movieId, cancellationToken);
    }

    public async Task<int> GetLikesForMovieInPartyAsync(Guid watchPartyId, Guid movieId, CancellationToken cancellationToken)
    {
        return await _context.Swipes
            .CountAsync(s => s.WatchPartyId == watchPartyId
                && s.MovieId == movieId
                && s.IsLiked
                && s.PartyMember.IsActive, cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetSwipedMovieIdsForMemberAsync(Guid partyMemberId, CancellationToken cancellationToken)
    {
        return await _context.Swipes
            .Where(s => s.PartyMemberId == partyMemberId)
            .Select(s => s.MovieId)
            .ToListAsync(cancellationToken);
    }
}
