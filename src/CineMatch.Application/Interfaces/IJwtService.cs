using CineMatch.Domain.Models;

namespace CineMatch.Application.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(User user);
    }
}
