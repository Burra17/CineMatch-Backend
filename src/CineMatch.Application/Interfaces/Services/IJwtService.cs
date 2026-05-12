using CineMatch.Domain.Models;

namespace CineMatch.Application.Interfaces.Services;

public interface IJwtService
{
    string GenerateToken(User user);
}
