namespace CineMatch.Infrastructure.Database.Configurations;

public class AdminSeedSettings
{
    public const string SectionName = "AdminSeed";
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
