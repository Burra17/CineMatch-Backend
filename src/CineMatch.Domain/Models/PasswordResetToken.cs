namespace CineMatch.Domain.Models;

public class PasswordResetToken
{
    // Identity
    public Guid Id { get; init; }

    // Foreign key
    public Guid UserId { get; set; }

    // State
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }

    // Audit
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UsedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
}
