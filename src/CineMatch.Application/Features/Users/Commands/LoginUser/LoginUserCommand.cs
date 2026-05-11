using CineMatch.Application.Features.Users.Common.Dtos;
using ErrorOr;
using MediatR;

namespace CineMatch.Application.Features.Users.Commands.LoginUser
{
    public record LoginUserCommand(string Email, string Password) : IRequest<ErrorOr<LoginResponseDto>>;
}
