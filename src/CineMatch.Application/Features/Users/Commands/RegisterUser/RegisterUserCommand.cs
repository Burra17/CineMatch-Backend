using CineMatch.Application.Features.Users.Common.Dtos;
using ErrorOr;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace CineMatch.Application.Features.Users.Commands.RegisterUser
{
    public record RegisterUserCommand(
        string Username,
        string Email,
        string Password
    ) : IRequest<ErrorOr<UserDto>>;
        
}
