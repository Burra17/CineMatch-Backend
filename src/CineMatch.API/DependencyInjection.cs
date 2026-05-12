using CineMatch.API.Services;
using CineMatch.Application.Interfaces.Services;

namespace CineMatch.API;

// API-layer DI registrations. Application and Infrastructure have their own AddXxx extensions —
// Program.cs only wires them together so service registration stays out of startup.
public static class DependencyInjection
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        // Required by CurrentUserService to reach the active HttpContext from outside a controller.
        services.AddHttpContextAccessor();

        services.AddScoped<ICurrentUserService, CurrentUserService>();
        return services;
    }
}
