using CineMatch.Application.Interfaces;
using CineMatch.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace CineMatch.Infrastructure.Database.Repositories
{
    public class WatchPartyRepository : GenericRepository<WatchParty>, IWatchPartyRepository
    {
        public WatchPartyRepository(AppDbContext context) : base(context)
        {

        }

        public async Task<WatchParty?> GetByIdWithMembersAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _context.WatchParties
                .Include(wp => wp.Host)
                .Include(wp => wp.PartyMembers)
                .ThenInclude(pm => pm.User)
                .FirstOrDefaultAsync(wp => wp.Id == id, cancellationToken);
        }

        public async Task<WatchParty?> GetByJoinCodeAsync(string joinCode, CancellationToken cancellationToken)
        {
            return await _context.WatchParties.FirstOrDefaultAsync(wp => wp.JoinCode == joinCode && wp.IsActive, cancellationToken);
        }

        public async Task<bool> IsJoinCodeUniqueAsync(string joinCode, CancellationToken cancellationToken)
        {
            return await _context.WatchParties.AllAsync(wp => wp.JoinCode != joinCode, cancellationToken);
        }
    }
}
