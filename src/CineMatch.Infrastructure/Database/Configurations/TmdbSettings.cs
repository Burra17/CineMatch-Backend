namespace CineMatch.Infrastructure.Database.Configurations;

public class TmdbSettings
{
    public const string SectionName = "Tmdb";
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string DefaultLanguage { get; set; } = "en-US";
}
