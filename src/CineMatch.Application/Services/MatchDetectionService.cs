using CineMatch.Application.Interfaces.Repositories;
using CineMatch.Application.Interfaces.Services;
using CineMatch.Domain.Models;

namespace CineMatch.Application.Services;

public class MatchDetectionService : IMatchDetectionService
{
    private readonly ISwipeRepository _swipeRepository;
    private readonly IMatchRepository _matchRepository;
    private readonly IPartyMemberRepository _partyMemberRepository;

    public MatchDetectionService(
        ISwipeRepository swipeRepository,
        IMatchRepository matchRepository,
        IPartyMemberRepository partyMemberRepository)
    {
        _swipeRepository = swipeRepository;
        _matchRepository = matchRepository;
        _partyMemberRepository = partyMemberRepository;
    }

    public async Task<Match?> DetectMatchAsync(Guid watchPartyId, Guid movieId, CancellationToken cancellationToken)
    {
        var members = await _partyMemberRepository.GetByPartyIdAsync(watchPartyId, cancellationToken);
        var activeMemberCount = members.Count(m => m.IsActive);

        if (activeMemberCount == 0)
            return null;

        var likeCount = await _swipeRepository.GetLikesForMovieInPartyAsync(watchPartyId, movieId, cancellationToken);

        if (likeCount != activeMemberCount)
            return null;

        var alreadyMatched = await _matchRepository.ExistsForMovieInPartyAsync(watchPartyId, movieId, cancellationToken);

        if (alreadyMatched)
            return null;

        return new Match
        {
            Id = Guid.NewGuid(),
            WatchPartyId = watchPartyId,
            MovieId = movieId,
            IsWatched = false
        };
    }
}
