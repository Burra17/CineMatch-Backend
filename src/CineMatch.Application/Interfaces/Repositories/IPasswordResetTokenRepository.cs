using CineMatch.Domain.Models;

namespace CineMatch.Application.Interfaces.Repositories;

public interface IPasswordResetTokenRepository : IGenericRepository<PasswordResetToken>
{
    Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);
}
