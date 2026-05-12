using CineMatch.Domain.Enums;

namespace CineMatch.Application.Features.Users.Common.Dtos;

public record UserDto(
    Guid Id,
    string Username,
    string Email,
    UserRole Role,
    DateTime CreatedAt
);
