namespace CineMatch.Domain.Models;

public class Swipe
{
    // Identity
    public Guid Id { get; init; }

    // Foreign keys
    public Guid PartyMemberId { get; init; }
    public Guid WatchPartyId { get; init; }
    public Guid MovieId { get; init; }

    // State
    public bool IsLiked { get; init; }
    public DateTime SwipedAt { get; init; } = DateTime.UtcNow;

    // Navigation properties
    public PartyMember PartyMember { get; set; } = null!;
    public WatchParty WatchParty { get; set; } = null!;
    public Movie Movie { get; set; } = null!;
}
