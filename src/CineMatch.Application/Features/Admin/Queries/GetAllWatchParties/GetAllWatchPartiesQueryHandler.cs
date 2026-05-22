using CineMatch.Application.Features.Admin.Common.Dtos;
using CineMatch.Application.Interfaces.Repositories;
using ErrorOr;
using MediatR;

namespace CineMatch.Application.Features.Admin.Queries.GetAllWatchParties;

public class GetAllWatchPartiesQueryHandler : IRequestHandler<GetAllWatchPartiesQuery, ErrorOr<List<AdminWatchPartyDto>>>
{
    private readonly IWatchPartyRepository _watchPartyRepository;

    public GetAllWatchPartiesQueryHandler(IWatchPartyRepository watchPartyRepository)
    {
        _watchPartyRepository = watchPartyRepository;
    }

    public async Task<ErrorOr<List<AdminWatchPartyDto>>> Handle(
        GetAllWatchPartiesQuery request,
        CancellationToken cancellationToken)
    {
        var watchParties = await _watchPartyRepository.GetAllWithHostAndMembersAsync(cancellationToken);

        var dtos = watchParties
            .Select(wp => new AdminWatchPartyDto(
                wp.Id,
                wp.JoinCode,
                wp.Host.Username,
                wp.PartyMembers.Count(pm => pm.IsActive),
                wp.IsActive,
                wp.CreatedAt))
            .ToList();

        return dtos;
    }
}
