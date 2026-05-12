using CineMatch.Application.Interfaces.Repositories;
using CineMatch.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace CineMatch.Infrastructure.Database.Repositories;

public class MovieRepository : GenericRepository<Movie>, IMovieRepository
{
    public MovieRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Movie?> GetByTmdbIdAsync(int tmdbId, CancellationToken cancellationToken)
    {
        return await _context.Movies.FirstOrDefaultAsync(m => m.TmdbId == tmdbId, cancellationToken);
    }

    public async Task BulkInsertAsync(IEnumerable<Movie> movies, CancellationToken cancellationToken)
    {
        await _context.Movies.AddRangeAsync(movies, cancellationToken);
    }
}
