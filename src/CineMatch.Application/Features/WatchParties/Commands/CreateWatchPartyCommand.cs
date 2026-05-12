using CineMatch.Application.Features.WatchParties.Common.Dtos;
using ErrorOr;
using MediatR;

namespace CineMatch.Application.Features.WatchParties.Commands
{
    public record CreateWatchPartyCommand(string Genre) : IRequest<ErrorOr<WatchPartyDto>>;
}
