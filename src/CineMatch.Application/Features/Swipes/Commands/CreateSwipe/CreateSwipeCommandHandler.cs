using AutoMapper;
using CineMatch.Application.Features.Swipes.Common.Dtos;
using CineMatch.Application.Features.Swipes.Common.Errors;
using CineMatch.Application.Interfaces.Repositories;
using CineMatch.Application.Interfaces.Services;
using CineMatch.Domain.Models;
using ErrorOr;
using MediatR;

namespace CineMatch.Application.Features.Swipes.Commands.CreateSwipe;

public class CreateSwipeCommandHandler : IRequestHandler<CreateSwipeCommand, ErrorOr<SwipeResultDto>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IPartyMemberRepository _partyMemberRepository;
    private readonly ISwipeRepository _swipeRepository;
    private readonly IWatchPartyMovieRepository _watchPartyMovieRepository;
    private readonly IMatchRepository _matchRepository;
    private readonly IMatchDetectionService _matchDetectionService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateSwipeCommandHandler(
        ICurrentUserService currentUserService,
        IPartyMemberRepository partyMemberRepository,
        ISwipeRepository swipeRepository,
        IWatchPartyMovieRepository watchPartyMovieRepository,
        IMatchRepository matchRepository,
        IMatchDetectionService matchDetectionService,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _currentUserService = currentUserService;
        _partyMemberRepository = partyMemberRepository;
        _swipeRepository = swipeRepository;
        _watchPartyMovieRepository = watchPartyMovieRepository;
        _matchRepository = matchRepository;
        _matchDetectionService = matchDetectionService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ErrorOr<SwipeResultDto>> Handle(CreateSwipeCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return SwipeErrors.Unauthorized;

        var partyMember = await _partyMemberRepository.GetMembershipAsync(userId.Value, request.WatchPartyId, cancellationToken);
        if (partyMember is null || !partyMember.IsActive)
            return SwipeErrors.NotMemberOfParty;

        var existingSwipe = await _swipeRepository.GetByPartyMemberAndMovieAsync(partyMember.Id, request.MovieId, cancellationToken);
        if (existingSwipe is not null)
            return SwipeErrors.AlreadySwiped;

        var partyMovies = await _watchPartyMovieRepository.GetMoviesForPartyAsync(request.WatchPartyId, cancellationToken);
        var movie = partyMovies.FirstOrDefault(m => m.Id == request.MovieId);
        if (movie is null)
            return SwipeErrors.MovieNotInParty;

        var swipe = new Swipe
        {
            Id = Guid.NewGuid(),
            PartyMemberId = partyMember.Id,
            WatchPartyId = request.WatchPartyId,
            MovieId = request.MovieId,
            IsLiked = request.IsLiked,
            SwipedAt = DateTime.UtcNow
        };
        await _swipeRepository.AddAsync(swipe);

        Match? match = null;
        if (request.IsLiked)
        {
            match = await _matchDetectionService.DetectMatchAsync(request.WatchPartyId, request.MovieId, cancellationToken);
            if (match is not null)
                await _matchRepository.AddAsync(match);
        }

        await _unitOfWork.SaveChangesAsync();

        var matchedMovieDto = match is not null ? _mapper.Map<MovieDto>(movie) : null;
        return new SwipeResultDto(IsMatch: match is not null, MatchedMovie: matchedMovieDto);
    }
}
