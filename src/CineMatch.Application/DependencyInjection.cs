using CineMatch.Application.Common.Behaviours;
using CineMatch.Application.Interfaces.Services;
using CineMatch.Application.Services;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace CineMatch.Application;

// Application-layer DI registrations: MediatR handlers, FluentValidation validators, pipeline behaviours, and AutoMapper profiles.
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // Pipeline behaviour order matters: LoggingBehaviour is registered first so it wraps ValidationBehaviour
        // (and the handler) — meaning validation failures are still observed in the logs.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));

        services.AddAutoMapper(cfg => cfg.AddMaps(typeof(DependencyInjection).Assembly));

        services.AddScoped<IMatchDetectionService, MatchDetectionService>();

        return services;
    }
}
