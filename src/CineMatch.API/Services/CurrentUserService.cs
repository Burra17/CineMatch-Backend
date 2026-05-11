using System.Security.Claims;
using CineMatch.Application.Interfaces;

namespace CineMatch.API.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _contextAccessor;

    public CurrentUserService(IHttpContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var userIdClaim = _contextAccessor.HttpContext?.User
                .FindFirstValue(ClaimTypes.NameIdentifier)
                ?? _contextAccessor.HttpContext?.User
                .FindFirstValue("sub");

            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }

    public string? Email =>
        _contextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email)
        ?? _contextAccessor.HttpContext?.User.FindFirstValue("email");
}