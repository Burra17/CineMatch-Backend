using CineMatch.Domain.Models;

namespace CineMatch.Application.Interfaces.Repositories;

public interface IMatchRepository : IGenericRepository<Match>
{
    Task<IReadOnlyList<Match>> GetByPartyAsync(Guid watchPartyId, CancellationToken cancellationToken);
    Task<bool> ExistsForMovieInPartyAsync(Guid watchPartyId, Guid movieId, CancellationToken cancellationToken);
}
