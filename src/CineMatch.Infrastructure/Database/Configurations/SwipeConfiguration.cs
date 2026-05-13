using CineMatch.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CineMatch.Infrastructure.Database.Configurations;

internal class SwipeConfiguration : IEntityTypeConfiguration<Swipe>
{
    public void Configure(EntityTypeBuilder<Swipe> builder)
    {
        builder.ToTable("Swipes");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.IsLiked)
            .IsRequired();

        builder.Property(s => s.SwipedAt)
            .IsRequired();

        // A member can only swipe once per movie within a party.
        builder.HasIndex(s => new { s.PartyMemberId, s.MovieId })
            .IsUnique();

        // Restrict: a member with swipe history cannot be deleted out from under those rows.
        builder.HasOne(s => s.PartyMember)
            .WithMany(pm => pm.Swipes)
            .HasForeignKey(s => s.PartyMemberId)
            .OnDelete(DeleteBehavior.Restrict);

        // Cascade: removing a party removes all its swipes — they have no meaning without a parent party.
        builder.HasOne(s => s.WatchParty)
            .WithMany(wp => wp.Swipes)
            .HasForeignKey(s => s.WatchPartyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict: a movie referenced by a swipe cannot be deleted.
        builder.HasOne(s => s.Movie)
            .WithMany(m => m.Swipes)
            .HasForeignKey(s => s.MovieId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
