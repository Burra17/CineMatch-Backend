namespace CineMatch.Domain.Models;

public class WatchParty
{
    // Identity
    public Guid Id { get; init; }
    public string JoinCode { get; set; } = string.Empty;

    // Configuration
    public Guid HostId { get; set; }
    public string Genre { get; set; } = "popular";

    // State
    public bool IsActive { get; set; } = true;
    public bool IsStarted { get; set; } = false;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    // Navigation properties
    public User Host { get; set; } = null!;
    public ICollection<PartyMember> PartyMembers { get; set; } = new List<PartyMember>();
    public ICollection<WatchPartyMovie> WatchPartyMovies { get; set; } = new List<WatchPartyMovie>();
    public ICollection<Swipe> Swipes { get; set; } = new List<Swipe>();
    public ICollection<Match> Matches { get; set; } = new List<Match>();
}
