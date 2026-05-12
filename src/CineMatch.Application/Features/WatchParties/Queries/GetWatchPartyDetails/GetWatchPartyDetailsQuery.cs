using CineMatch.Application.Features.WatchParties.Common.Dtos;
using ErrorOr;
using MediatR;

namespace CineMatch.Application.Features.WatchParties.Queries.GetWatchPartyDetails;

public record GetWatchPartyDetailsQuery(Guid WatchPartyId) : IRequest<ErrorOr<WatchPartyDetailsDto>>;
