using CineMatch.Domain.Enums;

namespace CineMatch.Domain.Models
{
    public class User
    {
        public Guid Id { get; init; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public bool IsEmailConfirmed { get; set; } = true;
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
