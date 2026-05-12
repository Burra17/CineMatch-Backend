using ErrorOr;
using MediatR;

namespace CineMatch.Application.Features.WatchParties.Commands.LeaveWatchParty;

public record LeaveWatchPartyCommand(Guid WatchPartyId) : IRequest<ErrorOr<Success>>;
