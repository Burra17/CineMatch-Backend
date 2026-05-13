using AutoMapper;
using CineMatch.Application.Features.Swipes.Common.Dtos;
using CineMatch.Application.Features.Swipes.Common.Errors;
using CineMatch.Application.Interfaces.Repositories;
using CineMatch.Application.Interfaces.Services;
using ErrorOr;
using MediatR;

namespace CineMatch.Application.Features.Swipes.Queries.GetSwipeQueue;

public class GetSwipeQueueQueryHandler : IRequestHandler<GetSwipeQueueQuery, ErrorOr<IReadOnlyList<MovieDto>>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IPartyMemberRepository _partyMemberRepository;
    private readonly ISwipeRepository _swipeRepository;
    private readonly IWatchPartyMovieRepository _watchPartyMovieRepository;
    private readonly IMapper _mapper;

    public GetSwipeQueueQueryHandler(
        ICurrentUserService currentUserService,
        IPartyMemberRepository partyMemberRepository,
        ISwipeRepository swipeRepository,
        IWatchPartyMovieRepository watchPartyMovieRepository,
        IMapper mapper)
    {
        _currentUserService = currentUserService;
        _partyMemberRepository = partyMemberRepository;
        _swipeRepository = swipeRepository;
        _watchPartyMovieRepository = watchPartyMovieRepository;
        _mapper = mapper;
    }

    public async Task<ErrorOr<IReadOnlyList<MovieDto>>> Handle(GetSwipeQueueQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return SwipeErrors.Unauthorized;

        var partyMember = await _partyMemberRepository.GetMembershipAsync(userId.Value, request.WatchPartyId, cancellationToken);
        if (partyMember is null || !partyMember.IsActive)
            return SwipeErrors.NotMemberOfParty;

        var swipedMovieIds = await _swipeRepository.GetSwipedMovieIdsForMemberAsync(partyMember.Id, cancellationToken);
        var partyMovies = await _watchPartyMovieRepository.GetMoviesForPartyAsync(request.WatchPartyId, cancellationToken);

        var queue = partyMovies
            .Where(m => !swipedMovieIds.Contains(m.Id))
            .Take(request.Count)
            .Select(m => _mapper.Map<MovieDto>(m))
            .ToList();

        return queue;
    }
}
