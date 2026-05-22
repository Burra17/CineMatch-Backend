using ErrorOr;
using MediatR;

namespace CineMatch.Application.Features.Admin.Queries.GetUserCount;

public record GetUserCountQuery : IRequest<ErrorOr<int>>;
