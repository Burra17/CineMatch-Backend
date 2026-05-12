using System.Security.Claims;
using CineMatch.Application.Interfaces;

namespace CineMatch.API.Services;

// Reads the current user from JWT claims on the active HttpContext.
// Returns null when no user is authenticated — handlers convert that to WatchPartyErrors.Unauthorized etc.
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
            // ASP.NET normally maps the JWT "sub" claim to ClaimTypes.NameIdentifier, but that
            // mapping can be disabled — check both so we don't break if MapInboundClaims is off.
            var userIdClaim = _contextAccessor.HttpContext?.User
                .FindFirstValue(ClaimTypes.NameIdentifier)
                ?? _contextAccessor.HttpContext?.User
                .FindFirstValue("sub");

            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }

    // Same dual-claim lookup as UserId — see the comment above for why we check both.
    public string? Email =>
        _contextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email)
        ?? _contextAccessor.HttpContext?.User.FindFirstValue("email");
}
