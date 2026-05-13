using ErrorOr;

namespace CineMatch.Application.Features.WatchParties.Common.Errors;

public static class WatchPartyErrors
{
    public static readonly Error NotFound = Error.NotFound(
        code: "WatchParty.NotFound",
        description: "The requested WatchParty was not found.");

    public static readonly Error JoinCodeNotFound = Error.NotFound(
        code: "WatchParty.JoinCodeNotFound",
        description: "The requested join code was not found.");

    public static readonly Error UserNotMember = Error.Forbidden(
        code: "WatchParty.UserNotMember",
        description: "The user is not a member of the requested WatchParty.");

    public static readonly Error HostCannotLeave = Error.Conflict(
        code: "WatchParty.HostCannotLeave",
        description: "The host is not allowed to leave the party.");

    public static readonly Error Unauthorized = Error.Unauthorized(
        code: "WatchParty.Unauthorized",
        description: "The user is unauthorized.");
}
