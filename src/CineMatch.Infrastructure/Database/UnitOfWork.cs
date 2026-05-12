using CineMatch.Application.Interfaces.Repositories;

namespace CineMatch.Infrastructure.Database;

// Thin wrapper around DbContext.SaveChangesAsync. Exists so handlers in the Application layer depend
// on IUnitOfWork (Application interface) instead of taking a direct dependency on EF Core's DbContext.
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}