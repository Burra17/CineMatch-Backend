using CineMatch.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CineMatch.Infrastructure.Database.Configurations;

internal class PartyMemberConfiguration : IEntityTypeConfiguration<PartyMember>
{
    public void Configure(EntityTypeBuilder<PartyMember> builder)
    {
        builder.ToTable("PartyMembers");

        builder.HasKey(pm => pm.Id);

        builder.Property(pm => pm.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(pm => pm.JoinedAt)
            .IsRequired();

        builder.Property(pm => pm.LeftAt)
            .IsRequired(false);

        // Composite unique constraint enforces one membership row per (user, party).
        // Combined with the soft-delete pattern (IsActive=false), this is why JoinWatchPartyCommandHandler
        // reactivates an existing row instead of inserting a duplicate.
        builder.HasIndex(pm => new { pm.UserId, pm.WatchPartyId })
            .IsUnique();

        // Restrict: a user with membership history can't be deleted out from under their party rows.
        builder.HasOne(pm => pm.User)
            .WithMany(u => u.PartyMemberships)
            .HasForeignKey(pm => pm.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Cascade: if a party is hard-deleted, its memberships go with it — they have no parent otherwise.
        builder.HasOne(pm => pm.WatchParty)
            .WithMany(wp => wp.PartyMembers)
            .HasForeignKey(pm => pm.WatchPartyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
