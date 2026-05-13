namespace CineMatch.Domain.Models;

public class Movie
{
    // Identity
    public Guid Id { get; init; }
    public int TmdbId { get; set; }

    // Info
    public string Title { get; set; } = string.Empty;
    public string PosterUrl { get; set; } = string.Empty;
    public string Overview { get; set; } = string.Empty;
    public int ReleaseYear { get; set; }

    // State
    public DateTime CachedAt { get; init; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<WatchPartyMovie> WatchPartyMovies { get; set; } = new List<WatchPartyMovie>();
}
