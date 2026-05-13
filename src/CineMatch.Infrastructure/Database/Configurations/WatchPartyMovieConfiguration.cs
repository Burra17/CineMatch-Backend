using CineMatch.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CineMatch.Infrastructure.Database.Configurations;

internal class WatchPartyMovieConfiguration : IEntityTypeConfiguration<WatchPartyMovie>
{
    public void Configure(EntityTypeBuilder<WatchPartyMovie> builder)
    {
        builder.ToTable("WatchPartyMovies");

        builder.HasKey(wpm => wpm.Id);

        builder.Property(wpm => wpm.OrderIndex)
            .IsRequired();

        builder.Property(wpm => wpm.AddedAt)
            .IsRequired();

        // A movie can only appear once per party.
        builder.HasIndex(wpm => new { wpm.WatchPartyId, wpm.MovieId })
            .IsUnique();

        // Each position in a party's queue must be occupied by exactly one movie.
        builder.HasIndex(wpm => new { wpm.WatchPartyId, wpm.OrderIndex })
            .IsUnique();

        // Cascade: removing a party removes its movie queue — rows are meaningless without a parent party.
        builder.HasOne(wpm => wpm.WatchParty)
            .WithMany(wp => wp.WatchPartyMovies)
            .HasForeignKey(wpm => wpm.WatchPartyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict: a movie that is part of a party queue cannot be deleted out from under it.
        builder.HasOne(wpm => wpm.Movie)
            .WithMany(m => m.WatchPartyMovies)
            .HasForeignKey(wpm => wpm.MovieId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
