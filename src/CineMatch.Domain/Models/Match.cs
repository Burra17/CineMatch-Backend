namespace CineMatch.Domain.Models;

public class Match
{
    // Identity
    public Guid Id { get; init; }

    // Foreign keys
    public Guid WatchPartyId { get; init; }
    public Guid MovieId { get; init; }

    // State
    public DateTime MatchedAt { get; init; } = DateTime.UtcNow;
    public bool IsWatched { get; set; }
    public Guid? WatchedByUserId { get; set; }
    public DateTime? WatchedAt { get; set; }

    // Navigation properties
    public WatchParty WatchParty { get; set; } = null!;
    public Movie Movie { get; set; } = null!;
    public User? WatchedBy { get; set; }
}
