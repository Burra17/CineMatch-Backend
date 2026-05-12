using CineMatch.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace CineMatch.Infrastructure.Database
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<WatchParty> WatchParties => Set<WatchParty>();
        public DbSet<PartyMember> PartyMembers => Set<PartyMember>();
        public DbSet<Movie> Movies => Set<Movie>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
        }
    }
}
