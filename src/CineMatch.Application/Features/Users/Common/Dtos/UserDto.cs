using CineMatch.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace CineMatch.Application.Features.Users.Common.Dtos
{
    public record UserDto(
        Guid Id,
        string Username,
        string Email, 
        UserRole Role,
        DateTime CreatedAt
    );
}
