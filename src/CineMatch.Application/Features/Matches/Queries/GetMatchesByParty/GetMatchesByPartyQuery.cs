using CineMatch.Application.Features.Matches.Common.Dtos;
using ErrorOr;
using MediatR;

namespace CineMatch.Application.Features.Matches.Queries.GetMatchesByParty;

public record GetMatchesByPartyQuery(Guid WatchPartyId) : IRequest<ErrorOr<IReadOnlyList<MatchDto>>>;
