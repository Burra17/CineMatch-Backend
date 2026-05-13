using CineMatch.Domain.Models;

namespace CineMatch.Application.Interfaces.Repositories;

public interface ISwipeRepository : IGenericRepository<Swipe>
{
    Task<Swipe?> GetByPartyMemberAndMovieAsync(Guid partyMemberId, Guid movieId, CancellationToken cancellationToken);
    Task<int> GetLikesForMovieInPartyAsync(Guid watchPartyId, Guid movieId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> GetSwipedMovieIdsForMemberAsync(Guid partyMemberId, CancellationToken cancellationToken);
}
