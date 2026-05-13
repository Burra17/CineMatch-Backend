using CineMatch.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CineMatch.Infrastructure.Database.Configurations;

internal class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("PasswordResetTokens");

        builder.HasKey(t => t.Id);

        // SHA-256 hex strings are always exactly 64 characters.
        builder.Property(t => t.TokenHash)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(t => t.ExpiresAt)
            .IsRequired();

        // Defaults to false so newly issued tokens are always valid until explicitly used.
        builder.Property(t => t.IsUsed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(t => t.UsedAt)
            .IsRequired(false);

        // Each hash value must be globally unique — lookup is always by hash, never by raw token.
        builder.HasIndex(t => t.TokenHash)
            .IsUnique();

        // Cascade: removing a user removes all their reset tokens — orphaned tokens are meaningless.
        builder.HasOne(t => t.User)
            .WithMany(u => u.PasswordResetTokens)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
