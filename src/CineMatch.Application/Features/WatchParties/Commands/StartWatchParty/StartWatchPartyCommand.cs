using ErrorOr;
using MediatR;

namespace CineMatch.Application.Features.WatchParties.Commands.StartWatchParty;

public record StartWatchPartyCommand(Guid WatchPartyId) : IRequest<ErrorOr<Success>>;
