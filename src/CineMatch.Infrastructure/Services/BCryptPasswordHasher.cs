using CineMatch.Application.Interfaces.Services;

namespace CineMatch.Infrastructure.Services;

public class BCryptPasswordHasher : IPasswordHasher
{
    // BCrypt cost factor — each +1 roughly doubles hashing time. 12 is the common 2024 baseline:
    // slow enough to deter brute force, fast enough to not block the login request noticeably.
    private const int WorkFactor = 12;

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
