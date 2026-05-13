using AutoMapper;
using CineMatch.Application.Features.Matches.Common.Dtos;
using CineMatch.Application.Features.Matches.Common.Errors;
using CineMatch.Application.Interfaces.Repositories;
using CineMatch.Application.Interfaces.Services;
using ErrorOr;
using MediatR;

namespace CineMatch.Application.Features.Matches.Commands.MarkMatchAsWatched;

public class MarkMatchAsWatchedCommandHandler : IRequestHandler<MarkMatchAsWatchedCommand, ErrorOr<MatchDto>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IMatchRepository _matchRepository;
    private readonly IPartyMemberRepository _partyMemberRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public MarkMatchAsWatchedCommandHandler(
        ICurrentUserService currentUserService,
        IMatchRepository matchRepository,
        IPartyMemberRepository partyMemberRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _currentUserService = currentUserService;
        _matchRepository = matchRepository;
        _partyMemberRepository = partyMemberRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ErrorOr<MatchDto>> Handle(MarkMatchAsWatchedCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return MatchErrors.Unauthorized;

        var match = await _matchRepository.GetByIdAsync(request.MatchId);
        if (match is null)
            return MatchErrors.NotFound;

        var partyMember = await _partyMemberRepository.GetMembershipAsync(userId.Value, match.WatchPartyId, cancellationToken);
        if (partyMember is null || !partyMember.IsActive)
            return MatchErrors.NotMemberOfParty;

        if (match.IsWatched)
            return MatchErrors.AlreadyWatched;

        match.IsWatched = true;
        match.WatchedByUserId = userId.Value;
        match.WatchedAt = DateTime.UtcNow;

        _matchRepository.Update(match);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<MatchDto>(match);
    }
}
