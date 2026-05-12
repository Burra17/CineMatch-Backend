using AutoMapper;
using CineMatch.Application.Features.WatchParties.Common.Dtos;
using CineMatch.Application.Features.WatchParties.Common.Errors;
using CineMatch.Application.Interfaces.Repositories;
using CineMatch.Application.Interfaces.Services;
using ErrorOr;
using MediatR;

namespace CineMatch.Application.Features.WatchParties.Queries.GetWatchPartyDetails;

// Only active members of the party can read its details. Non-members and previously-left members are blocked.
public class GetWatchPartyDetailsQueryHandler : IRequestHandler<GetWatchPartyDetailsQuery, ErrorOr<WatchPartyDetailsDto>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly IWatchPartyRepository _watchPartyRepository;

    public GetWatchPartyDetailsQueryHandler(
        ICurrentUserService currentUserService,
        IMapper mapper,
        IWatchPartyRepository watchPartyRepository)
    {
        _currentUserService = currentUserService;
        _mapper = mapper;
        _watchPartyRepository = watchPartyRepository;
    }

    public async Task<ErrorOr<WatchPartyDetailsDto>> Handle(GetWatchPartyDetailsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        if (userId is null)
        {
            return WatchPartyErrors.Unauthorized;
        }

        var party = await _watchPartyRepository.GetByIdWithMembersAsync(request.WatchPartyId, cancellationToken);

        if (party is null)
        {
            return WatchPartyErrors.NotFound;
        }

        // Membership check uses the eager-loaded PartyMembers collection (no extra repo round-trip).
        // The IsActive filter is intentional — members who previously left must not see details either.
        var isActiveMember = party.PartyMembers.Any(m => m.UserId == userId.Value && m.IsActive);

        if (!isActiveMember)
        {
            return WatchPartyErrors.UserNotMember;
        }

        return _mapper.Map<WatchPartyDetailsDto>(party);
    }
}
