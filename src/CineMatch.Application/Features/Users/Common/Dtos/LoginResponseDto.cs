namespace CineMatch.Application.Features.Users.Common.Dtos
{
    public record LoginResponseDto(string Token, UserDto User);
}
