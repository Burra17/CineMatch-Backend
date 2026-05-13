using CineMatch.Application.Features.Matches.Common.Dtos;
using ErrorOr;
using MediatR;

namespace CineMatch.Application.Features.Matches.Commands.MarkMatchAsWatched;

public record MarkMatchAsWatchedCommand(Guid MatchId) : IRequest<ErrorOr<MatchDto>>;
