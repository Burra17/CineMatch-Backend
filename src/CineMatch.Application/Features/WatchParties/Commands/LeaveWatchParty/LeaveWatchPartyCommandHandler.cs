using CineMatch.Application.Features.WatchParties.Common.Errors;
using CineMatch.Application.Interfaces.Repositories;
using CineMatch.Application.Interfaces.Services;
using ErrorOr;
using MediatR;

namespace CineMatch.Application.Features.WatchParties.Commands.LeaveWatchParty;

// Soft-deletes the membership (preserves history) and closes the party when the last active member leaves.
// The host can only leave if they are the last remaining active member — otherwise blocked, see below.
public class LeaveWatchPartyCommandHandler : IRequestHandler<LeaveWatchPartyCommand, ErrorOr<Success>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IPartyMemberRepository _partyMemberRepository;
    private readonly IWatchPartyRepository _watchPartyRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LeaveWatchPartyCommandHandler(
        ICurrentUserService currentUserService,
        IPartyMemberRepository partyMemberRepository,
        IWatchPartyRepository watchPartyRepository,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _partyMemberRepository = partyMemberRepository;
        _watchPartyRepository = watchPartyRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ErrorOr<Success>> Handle(LeaveWatchPartyCommand request, CancellationToken cancellationToken)
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

        var membership = await _partyMemberRepository.GetMembershipAsync(userId.Value, request.WatchPartyId, cancellationToken);

        if (membership is null || !membership.IsActive)
        {
            return WatchPartyErrors.UserNotMember;
        }

        var activeMemberCount = party.PartyMembers.Count(m => m.IsActive);
        var isHost = party.HostId == userId.Value;

        // Host-transfer is not yet implemented — until then, a host with other active members cannot leave
        // (would otherwise orphan the party). If the host is alone, the branch below closes the party instead.
        if (isHost && activeMemberCount > 1)
        {
            return WatchPartyErrors.HostCannotLeave;
        }

        membership.IsActive = false;
        membership.LeftAt = DateTime.UtcNow;

        if (activeMemberCount == 1)
        {
            party.IsActive = false;
            party.ClosedAt = DateTime.UtcNow;
        }

        await _unitOfWork.SaveChangesAsync();

        return Result.Success;
    }
}
