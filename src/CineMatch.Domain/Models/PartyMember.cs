namespace CineMatch.Domain.Models;

public class PartyMember
{
    // Identity
    public Guid Id { get; init; }

    // Relations
    public Guid UserId { get; set; }
    public Guid WatchPartyId { get; set; }

    // State
    public DateTime JoinedAt { get; init; } = DateTime.UtcNow;
    public DateTime? LeftAt { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public User User { get; set; } = null!;
    public WatchParty WatchParty { get; set; } = null!;
    public ICollection<Swipe> Swipes { get; set; } = new List<Swipe>();
}