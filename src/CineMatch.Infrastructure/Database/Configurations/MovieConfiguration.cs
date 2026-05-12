using CineMatch.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CineMatch.Infrastructure.Database.Configurations
{
    public class MovieConfiguration : IEntityTypeConfiguration<Movie>
    {
        public void Configure(EntityTypeBuilder<Movie> builder)
        {
            builder.ToTable("Movies");

            builder.HasKey(m => m.Id);

            builder.HasIndex(m => m.TmdbId)
                .IsUnique();

            builder.Property(m => m.TmdbId)
                .IsRequired();

            builder.Property(m => m.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(m => m.PosterUrl)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(m => m.Overview)
                .IsRequired()
                .HasMaxLength(2000);

            builder.Property(m => m.ReleaseYear)
                .IsRequired();

            builder.Property(m => m.CachedAt)
                .IsRequired();
        }
    }
}
