using CineMatch.Domain.Models;

namespace CineMatch.Application.Interfaces;

public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken);
    Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken);
}