using CineMatch.Application.Features.WatchParties.Common.Errors;
using CineMatch.Application.Interfaces.Repositories;
using CineMatch.Application.Interfaces.Services;
using ErrorOr;
using MediatR;

namespace CineMatch.Application.Features.WatchParties.Commands.StartWatchParty;

public class StartWatchPartyCommandHandler : IRequestHandler<StartWatchPartyCommand, ErrorOr<Success>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IWatchPartyRepository _watchPartyRepository;
    private readonly IUnitOfWork _unitOfWork;

    public StartWatchPartyCommandHandler(
        ICurrentUserService currentUserService,
        IWatchPartyRepository watchPartyRepository,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _watchPartyRepository = watchPartyRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ErrorOr<Success>> Handle(StartWatchPartyCommand request, CancellationToken cancellationToken)
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

        if (party.HostId != userId.Value)
        {
            return WatchPartyErrors.NotHost;
        }

        party.IsStarted = true;
        party.StartedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        return Result.Success;
    }
}
