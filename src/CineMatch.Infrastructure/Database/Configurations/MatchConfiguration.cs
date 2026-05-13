using CineMatch.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CineMatch.Infrastructure.Database.Configurations;

internal class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    public void Configure(EntityTypeBuilder<Match> builder)
    {
        builder.ToTable("Matches");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.MatchedAt)
            .IsRequired();

        // Defaults to false so newly created matches are always unwatched.
        builder.Property(m => m.IsWatched)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(m => m.WatchedByUserId)
            .IsRequired(false);

        builder.Property(m => m.WatchedAt)
            .IsRequired(false);

        // A movie can only match once per party.
        builder.HasIndex(m => new { m.WatchPartyId, m.MovieId })
            .IsUnique();

        // Cascade: removing a party removes its matches — they have no meaning without a parent party.
        builder.HasOne(m => m.WatchParty)
            .WithMany(wp => wp.Matches)
            .HasForeignKey(m => m.WatchPartyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict: a matched movie cannot be deleted out from under an existing match record.
        builder.HasOne(m => m.Movie)
            .WithMany(mv => mv.Matches)
            .HasForeignKey(m => m.MovieId)
            .OnDelete(DeleteBehavior.Restrict);

        // Restrict: a user who marked a match as watched cannot be deleted while that record exists.
        builder.HasOne(m => m.WatchedBy)
            .WithMany(u => u.WatchedMatches)
            .HasForeignKey(m => m.WatchedByUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
