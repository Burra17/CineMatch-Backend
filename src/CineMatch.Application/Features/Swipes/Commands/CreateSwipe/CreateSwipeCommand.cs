using CineMatch.Application.Features.Swipes.Common.Dtos;
using ErrorOr;
using MediatR;

namespace CineMatch.Application.Features.Swipes.Commands.CreateSwipe;

public record CreateSwipeCommand(Guid WatchPartyId, Guid MovieId, bool IsLiked) : IRequest<ErrorOr<SwipeResultDto>>;
