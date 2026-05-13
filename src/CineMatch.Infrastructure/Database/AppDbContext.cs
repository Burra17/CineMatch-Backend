using CineMatch.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace CineMatch.Infrastructure.Database;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<WatchParty> WatchParties => Set<WatchParty>();
    public DbSet<PartyMember> PartyMembers => Set<PartyMember>();
    public DbSet<Movie> Movies => Set<Movie>();
    public DbSet<WatchPartyMovie> WatchPartyMovies => Set<WatchPartyMovie>();
    public DbSet<Swipe> Swipes => Set<Swipe>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Auto-discovers every IEntityTypeConfiguration<T> in this assembly (the Configurations folder).
        // New configuration classes are picked up automatically — no explicit registration needed.
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
    }
}
