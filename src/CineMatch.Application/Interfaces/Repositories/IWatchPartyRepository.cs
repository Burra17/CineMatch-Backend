using CineMatch.Domain.Models;

namespace CineMatch.Application.Interfaces.Repositories;

public interface IWatchPartyRepository : IGenericRepository<WatchParty>
{
    Task<WatchParty?> GetByIdWithMembersAsync(Guid id, CancellationToken cancellationToken);
    Task<WatchParty?> GetByJoinCodeAsync(string joinCode, CancellationToken cancellationToken);
    Task<bool> IsJoinCodeUniqueAsync(string joinCode, CancellationToken cancellationToken);
    Task<List<WatchParty>> GetAllWithHostAndMembersAsync(CancellationToken cancellationToken);
}
