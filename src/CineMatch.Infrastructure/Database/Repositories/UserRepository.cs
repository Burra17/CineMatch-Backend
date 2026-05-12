using CineMatch.Domain.Models;
using CineMatch.Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;


namespace CineMatch.Infrastructure.Database.Repositories;

public class UserRepository : GenericRepository<User>, IUserRepository
{

    public UserRepository(AppDbContext context) : base(context)
    {
        
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken)
    {
        return await _context.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken);
    }

    public async Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        return await _context.Users.AnyAsync(u => u.Username == username, cancellationToken);
    }
}