using CineMatch.Application.Interfaces.Services;
using CineMatch.Domain.Enums;
using CineMatch.Domain.Models;
using CineMatch.Infrastructure.Database.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CineMatch.Infrastructure.Database;

public class ApplicationDbInitializer
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly AdminSeedSettings _settings;
    private readonly ILogger<ApplicationDbInitializer> _logger;

    public ApplicationDbInitializer(
        AppDbContext dbContext,
        IPasswordHasher passwordHasher,
        IOptions<AdminSeedSettings> settings,
        ILogger<ApplicationDbInitializer> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SeedAdminAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.Email) ||
            string.IsNullOrWhiteSpace(_settings.Username) ||
            string.IsNullOrWhiteSpace(_settings.Password))
        {
            _logger.LogInformation("AdminSeed credentials not configured — skipping admin seeding.");
            return;
        }

        var adminExists = await _dbContext.Users
            .AnyAsync(u => u.Role == UserRole.Admin, cancellationToken);

        if (adminExists)
        {
            return;
        }

        var admin = new User
        {
            Id = Guid.NewGuid(),
            Email = _settings.Email,
            Username = _settings.Username,
            PasswordHash = _passwordHasher.HashPassword(_settings.Password),
            Role = UserRole.Admin,
        };

        _dbContext.Users.Add(admin);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Admin user '{Username}' seeded successfully.", admin.Username);
    }
}

public static class ApplicationDbInitializerExtensions
{
    public static async Task SeedAdminAsync(this IServiceProvider serviceProvider)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var initializer = scope.ServiceProvider.GetRequiredService<ApplicationDbInitializer>();
        await initializer.SeedAdminAsync();
    }
}
