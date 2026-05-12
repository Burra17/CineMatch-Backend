namespace CineMatch.Application.Features.WatchParties.Common.Dtos
{
    public record PartyMemberDto(
        Guid UserId,
        string Username,
        DateTime JoinedAt,
        bool IsActive
    );
}
