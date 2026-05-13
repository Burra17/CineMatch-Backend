using ErrorOr;

namespace CineMatch.Application.Features.Swipes.Common.Errors;

public static class SwipeErrors
{
    public static readonly Error Unauthorized = Error.Unauthorized(
        code: "Swipe.Unauthorized",
        description: "The user is unauthorized.");

    public static readonly Error NotMemberOfParty = Error.Forbidden(
        code: "Swipe.NotMemberOfParty",
        description: "The user is not an active member of this party.");

    public static readonly Error AlreadySwiped = Error.Conflict(
        code: "Swipe.AlreadySwiped",
        description: "The user has already swiped on this movie in this party.");

    public static readonly Error MovieNotInParty = Error.Validation(
        code: "Swipe.MovieNotInParty",
        description: "The movie is not part of this party's queue.");
}
