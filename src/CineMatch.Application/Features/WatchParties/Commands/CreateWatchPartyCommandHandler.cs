using AutoMapper;
using CineMatch.Application.Features.WatchParties.Common.Dtos;
using CineMatch.Application.Features.WatchParties.Common.Errors;
using CineMatch.Application.Interfaces;
using CineMatch.Domain.Models;
using ErrorOr;
using MediatR;

namespace CineMatch.Application.Features.WatchParties.Commands
{
    public class CreateWatchPartyCommandHandler : IRequestHandler<CreateWatchPartyCommand, ErrorOr<WatchPartyDto>>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IJoinCodeGenerator _joinCodeGenerator;
        private readonly IWatchPartyRepository _watchPartyRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public CreateWatchPartyCommandHandler(
            ICurrentUserService currentUserService,
            IJoinCodeGenerator joinCodeGenerator,
            IWatchPartyRepository watchPartyRepository,
            IMapper mapper,
            IUnitOfWork unitOfWork)
        {
            _currentUserService = currentUserService;
            _joinCodeGenerator = joinCodeGenerator;
            _watchPartyRepository = watchPartyRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<ErrorOr<WatchPartyDto>> Handle(CreateWatchPartyCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            if (userId is null)
            {
                return WatchPartyErrors.Unauthorized;
            }

            var joinCode = await _joinCodeGenerator.GenerateUniqueCodeAsync(cancellationToken);

            var watchParty = new WatchParty
            {
                Id = Guid.NewGuid(),
                JoinCode = joinCode,
                HostId = userId.Value, 
                Genre = string.IsNullOrWhiteSpace(request.Genre) ? "popular" : request.Genre,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                PartyMembers = new List<PartyMember>()
            };

            var partyMember = new PartyMember
            {
                Id = Guid.NewGuid(),
                UserId = userId.Value,
                WatchPartyId = watchParty.Id,
                JoinedAt = DateTime.UtcNow,
                IsActive = true
            };

            watchParty.PartyMembers.Add(partyMember);

            await _watchPartyRepository.AddAsync(watchParty);
            await _unitOfWork.SaveChangesAsync();

            // 5. Mappa och returnera
            return _mapper.Map<WatchPartyDto>(watchParty);
        }
    }
}
