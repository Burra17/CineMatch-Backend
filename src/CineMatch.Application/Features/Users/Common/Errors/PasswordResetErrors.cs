using ErrorOr;

namespace CineMatch.Application.Features.Users.Common.Errors;

public static class PasswordResetErrors
{
    public static readonly Error InvalidOrExpiredToken = Error.Validation(
        code: "PasswordReset.InvalidOrExpiredToken",
        description: "The password reset token is invalid, expired, or has already been used");
}
