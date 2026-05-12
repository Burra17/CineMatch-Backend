using AutoMapper;
using CineMatch.Application.Features.WatchParties.Common.Dtos;
using CineMatch.Application.Features.WatchParties.Common.Errors;
using CineMatch.Application.Interfaces;
using CineMatch.Domain.Models;
using ErrorOr;
using MediatR;

namespace CineMatch.Application.Features.WatchParties.Commands.JoinWatchParty
{
    public class JoinWatchPartyCommandHandler : IRequestHandler<JoinWatchPartyCommand, ErrorOr<WatchPartyDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IPartyMemberRepository _partyMemberRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IWatchPartyRepository _watchPartyRepository;

        public JoinWatchPartyCommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IPartyMemberRepository partyMemberRepository,
            ICurrentUserService currentUserService,
            IWatchPartyRepository watchPartyRepository)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _partyMemberRepository = partyMemberRepository;
            _currentUserService = currentUserService;
            _watchPartyRepository = watchPartyRepository;
        }

        public async Task<ErrorOr<WatchPartyDto>> Handle(JoinWatchPartyCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (userId is null)
            { 
                return WatchPartyErrors.Unauthorized; 
            }

            var party = await _watchPartyRepository.GetByJoinCodeAsync(request.JoinCode, cancellationToken);
            if (party is null)
            {
                return WatchPartyErrors.JoinCodeNotFound;
            }

            var membership = await _partyMemberRepository.GetMembershipAsync(userId.Value, party.Id, cancellationToken);

            if (membership is not null && membership.IsActive)
            {
                var existingParty = await _watchPartyRepository.GetByIdWithMembersAsync(party.Id, cancellationToken);
                return _mapper.Map<WatchPartyDto>(existingParty);
            }
            else if (membership is not null)
            {
                // Previously left — reactivate
                membership.IsActive = true;
                membership.LeftAt = null;
                await _unitOfWork.SaveChangesAsync();
            }
            else
            {
                var newMember = new PartyMember
                {
                    Id = Guid.NewGuid(),
                    UserId = userId.Value,
                    WatchPartyId = party.Id,
                    JoinedAt = DateTime.UtcNow,
                    IsActive = true
                };
                await _partyMemberRepository.AddAsync(newMember);
                await _unitOfWork.SaveChangesAsync();
            }

            var partyWithMembers = await _watchPartyRepository.GetByIdWithMembersAsync(party.Id, cancellationToken);
            return _mapper.Map<WatchPartyDto>(partyWithMembers);
        }
    }
}
