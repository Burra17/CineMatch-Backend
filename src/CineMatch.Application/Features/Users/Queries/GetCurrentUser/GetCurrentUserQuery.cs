using CineMatch.Application.Features.Users.Common.Dtos;
using ErrorOr;
using MediatR;

namespace CineMatch.Application.Features.Users.Queries.GetCurrentUser;

public record GetCurrentUserQuery : IRequest<ErrorOr<UserDto>>;
