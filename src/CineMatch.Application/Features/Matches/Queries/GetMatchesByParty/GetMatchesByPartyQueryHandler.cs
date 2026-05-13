using AutoMapper;
using CineMatch.Application.Features.Matches.Common.Dtos;
using CineMatch.Application.Features.Matches.Common.Errors;
using CineMatch.Application.Interfaces.Repositories;
using CineMatch.Application.Interfaces.Services;
using ErrorOr;
using MediatR;

namespace CineMatch.Application.Features.Matches.Queries.GetMatchesByParty;

public class GetMatchesByPartyQueryHandler : IRequestHandler<GetMatchesByPartyQuery, ErrorOr<IReadOnlyList<MatchDto>>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IPartyMemberRepository _partyMemberRepository;
    private readonly IMatchRepository _matchRepository;
    private readonly IMapper _mapper;

    public GetMatchesByPartyQueryHandler(
        ICurrentUserService currentUserService,
        IPartyMemberRepository partyMemberRepository,
        IMatchRepository matchRepository,
        IMapper mapper)
    {
        _currentUserService = currentUserService;
        _partyMemberRepository = partyMemberRepository;
        _matchRepository = matchRepository;
        _mapper = mapper;
    }

    public async Task<ErrorOr<IReadOnlyList<MatchDto>>> Handle(GetMatchesByPartyQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
            return MatchErrors.Unauthorized;

        var partyMember = await _partyMemberRepository.GetMembershipAsync(userId.Value, request.WatchPartyId, cancellationToken);
        if (partyMember is null || !partyMember.IsActive)
            return MatchErrors.NotMemberOfParty;

        var matches = await _matchRepository.GetByPartyAsync(request.WatchPartyId, cancellationToken);

        return matches.Select(m => _mapper.Map<MatchDto>(m)).ToList();
    }
}
