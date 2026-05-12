using CineMatch.Domain.Enums;

namespace CineMatch.Domain.Models;

public class User
{
    // Identity
    public Guid Id { get; init; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // Auth
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsEmailConfirmed { get; set; } = true;

    // Audit
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<WatchParty> WatchPartiesHosted { get; set; } = new List<WatchParty>();
    public ICollection<PartyMember> PartyMemberships { get; set; } = new List<PartyMember>();
}