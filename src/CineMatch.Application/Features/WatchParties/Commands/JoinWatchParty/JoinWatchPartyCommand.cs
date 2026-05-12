using CineMatch.Application.Features.WatchParties.Common.Dtos;
using ErrorOr;
using MediatR;

namespace CineMatch.Application.Features.WatchParties.Commands.JoinWatchParty;

public record JoinWatchPartyCommand(string JoinCode) : IRequest<ErrorOr<WatchPartyDto>>;
