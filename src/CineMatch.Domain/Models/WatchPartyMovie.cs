namespace CineMatch.Domain.Models;

public class WatchPartyMovie
{
    // Identity
    public Guid Id { get; init; }

    // Foreign keys
    public Guid WatchPartyId { get; init; }
    public Guid MovieId { get; init; }

    // Ordering
    public int OrderIndex { get; init; }

    // State
    public DateTime AddedAt { get; init; } = DateTime.UtcNow;

    // Navigation properties
    public WatchParty WatchParty { get; set; } = null!;
    public Movie Movie { get; set; } = null!;
}
