using CineMatch.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CineMatch.Infrastructure.Database.Configurations;

internal class WatchPartyConfiguration : IEntityTypeConfiguration<WatchParty>
{
    public void Configure(EntityTypeBuilder<WatchParty> builder)
    {
        builder.ToTable("WatchParties");

        builder.HasKey(wp => wp.Id);

        builder.Property(wp => wp.JoinCode)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(wp => wp.Genre)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(wp => wp.IsActive)
            .IsRequired();

        builder.Property(wp => wp.CreatedAt)
            .IsRequired();

        builder.Property(wp => wp.ClosedAt)
            .IsRequired(false);

        // Filtered unique index — JoinCode only needs to be unique among active parties,
        // which lets inactive parties' codes be reused for new ones.
        builder.HasIndex(wp => wp.JoinCode)
            .IsUnique()
            .HasFilter("\"IsActive\" = true");

        // Restrict deletion of a user who still hosts parties — would otherwise orphan them.
        builder.HasOne(wp => wp.Host)
            .WithMany(u => u.WatchPartiesHosted)
            .HasForeignKey(wp => wp.HostId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
