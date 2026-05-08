using ErrorOr;

namespace CineMatch.Application.Features.Users.Common.Errors;

public static class UserErrors
{
    public static Error EmailAlreadyExists => Error.Conflict(
        code: "User.EmailAlreadyExists",
        description: "An account with this email already exists");

    public static Error UsernameAlreadyExists => Error.Conflict(
        code: "User.UsernameAlreadyExists",
        description: "This username is already taken");

    public static Error InvalidCredentials => Error.Unauthorized(
        code: "Auth.InvalidCredentials",
        description: "Invalid email or password");

    public static Error NotFound => Error.NotFound(
        code: "User.NotFound",
        description: "The requested user was not found");
}