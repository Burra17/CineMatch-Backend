using CineMatch.Application.Interfaces;
using CineMatch.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace CineMatch.Infrastructure.Database.Repositories
{
    public class PartyMemberRepository : GenericRepository<PartyMember>, IPartyMemberRepository
    {
        public PartyMemberRepository(AppDbContext context) : base(context)
        {

        }

        public async Task<IReadOnlyList<PartyMember>> GetByPartyIdAsync(Guid partyId, CancellationToken cancellationToken)
        {
            return await _context.PartyMembers.Where(pm => pm.WatchPartyId == partyId && pm.IsActive).ToListAsync(cancellationToken);
        }

        public async Task<PartyMember?> GetMembershipAsync(Guid userId, Guid partyId, CancellationToken cancellationToken)
        {
            return await _context.PartyMembers.FirstOrDefaultAsync(pm => pm.UserId == userId && pm.WatchPartyId == partyId, cancellationToken);
        }

        public async Task<bool> IsUserMemberOfPartyAsync(Guid userId, Guid partyId, CancellationToken cancellationToken)
        {
            return await _context.PartyMembers.AnyAsync(pm => pm.UserId == userId && pm.WatchPartyId == partyId && pm.IsActive, cancellationToken);
        }
    }
}
