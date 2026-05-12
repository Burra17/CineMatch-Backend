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

        // Composite unique constraint — användare kan bara vara med i party en gång
        builder.HasIndex(pm => new { pm.UserId, pm.WatchPartyId })
            .IsUnique();

        // Relation till User
        builder.HasOne(pm => pm.User)
            .WithMany(u => u.PartyMemberships)
            .HasForeignKey(pm => pm.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relation till WatchParty (Cascade så medlemskap raderas om partyt raderas)
        builder.HasOne(pm => pm.WatchParty)
            .WithMany(wp => wp.PartyMembers)
            .HasForeignKey(pm => pm.WatchPartyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}