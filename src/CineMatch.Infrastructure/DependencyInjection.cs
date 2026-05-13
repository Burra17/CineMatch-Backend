using CineMatch.Application.Interfaces.Repositories;
using CineMatch.Application.Interfaces.Services;
using CineMatch.Infrastructure.Database;
using CineMatch.Infrastructure.Database.Configurations;
using CineMatch.Infrastructure.Database.Repositories;
using CineMatch.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace CineMatch.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Repositories
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IWatchPartyRepository, WatchPartyRepository>();
        services.AddScoped<IPartyMemberRepository, PartyMemberRepository>();
        services.AddScoped<IMovieRepository, MovieRepository>();
        services.AddScoped<ISwipeRepository, SwipeRepository>();
        services.AddScoped<IMatchRepository, MatchRepository>();
        services.AddScoped<IWatchPartyMovieRepository, WatchPartyMovieRepository>();

        // Services
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IJoinCodeGenerator, JoinCodeGenerator>();

        AddJwtAuthentication(services, configuration);

        return services;
    }

    private static void AddJwtAuthentication(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        var jwtSettings = configuration
            .GetSection(JwtSettings.SectionName)
            .Get<JwtSettings>()
            ?? throw new InvalidOperationException("JwtSettings is not configured");

        if (string.IsNullOrWhiteSpace(jwtSettings.Secret) || jwtSettings.Secret.Length < 32)
        {
            throw new InvalidOperationException(
                "JwtSettings:Secret must be at least 32 characters. " +
                "Set it via user secrets: dotnet user-secrets set ");
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();
    }
}
