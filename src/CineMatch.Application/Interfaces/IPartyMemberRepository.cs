using CineMatch.Domain.Models;

namespace CineMatch.Application.Interfaces
{
    public interface IPartyMemberRepository : IGenericRepository<PartyMember>
    {
        Task<IReadOnlyList<PartyMember>> GetByPartyIdAsync(Guid partyId, CancellationToken cancellationToken);
        Task<bool> IsUserMemberOfPartyAsync(Guid userId, Guid partyId, CancellationToken cancellationToken);
        Task<PartyMember?> GetMembershipAsync(Guid userId, Guid partyId, CancellationToken cancellationToken);
    }
}
