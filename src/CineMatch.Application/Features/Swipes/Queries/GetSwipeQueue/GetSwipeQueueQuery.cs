using CineMatch.Application.Features.Swipes.Common.Dtos;
using ErrorOr;
using MediatR;

namespace CineMatch.Application.Features.Swipes.Queries.GetSwipeQueue;

public record GetSwipeQueueQuery(Guid WatchPartyId, int Count = 10) : IRequest<ErrorOr<IReadOnlyList<MovieDto>>>;
