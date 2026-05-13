using AutoMapper;
using CineMatch.Application.Features.WatchParties.Common.Dtos;
using CineMatch.Application.Features.WatchParties.Common.Errors;
using CineMatch.Application.Interfaces.Repositories;
using CineMatch.Application.Interfaces.Services;
using CineMatch.Domain.Models;
using ErrorOr;
using MediatR;

namespace CineMatch.Application.Features.WatchParties.Commands.CreateWatchParty;

public class CreateWatchPartyCommandHandler : IRequestHandler<CreateWatchPartyCommand, ErrorOr<WatchPartyDto>>
{
    // Genre selection is not exposed to clients yet — all parties default to "popular" until we add real genre picking.
    private const string DefaultGenre = "popular";

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
            Genre = DefaultGenre,
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

        // Reload with .Include(Host) and .Include(PartyMembers) so AutoMapper can resolve HostUsername;
        // the freshly-constructed entity has HostId set but Host navigation property is null.
        var partyWithRelations = await _watchPartyRepository.GetByIdWithMembersAsync(watchParty.Id, cancellationToken);
        return _mapper.Map<WatchPartyDto>(partyWithRelations);
    }
}
