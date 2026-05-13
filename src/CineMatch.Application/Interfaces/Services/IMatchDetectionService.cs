using CineMatch.Domain.Models;

namespace CineMatch.Application.Interfaces.Services;

public interface IMatchDetectionService
{
    Task<Match?> DetectMatchAsync(Guid watchPartyId, Guid movieId, CancellationToken cancellationToken);
}
