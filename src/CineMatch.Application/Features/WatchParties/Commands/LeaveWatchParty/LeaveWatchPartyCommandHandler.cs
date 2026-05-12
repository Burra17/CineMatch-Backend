using CineMatch.Application.Features.WatchParties.Common.Errors;
using CineMatch.Application.Interfaces;
using ErrorOr;
using MediatR;

namespace CineMatch.Application.Features.WatchParties.Commands.LeaveWatchParty
{
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
}
