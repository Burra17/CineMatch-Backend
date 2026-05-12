using CineMatch.Application.Features.WatchParties.Common.Dtos;
using ErrorOr;
using MediatR;

namespace CineMatch.Application.Features.WatchParties.Commands.CreateWatchParty
{
    public record CreateWatchPartyCommand : IRequest<ErrorOr<WatchPartyDto>>;
}
