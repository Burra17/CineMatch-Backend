using ErrorOr;

namespace CineMatch.Application.Features.Matches.Common.Errors;

public static class MatchErrors
{
    public static readonly Error Unauthorized = Error.Unauthorized(
        code: "Match.Unauthorized",
        description: "The user is unauthorized.");

    public static readonly Error NotFound = Error.NotFound(
        code: "Match.NotFound",
        description: "The requested match was not found.");

    public static readonly Error NotMemberOfParty = Error.Forbidden(
        code: "Match.NotMemberOfParty",
        description: "The user is not an active member of this party.");

    public static readonly Error AlreadyWatched = Error.Conflict(
        code: "Match.AlreadyWatched",
        description: "This match has already been marked as watched.");
}
