using ErrorOr;
using Microsoft.AspNetCore.Mvc;

namespace CineMatch.API.Common;

// Maps handler results (ErrorOr<T>) to HTTP responses. Controllers call
// result.ToActionResult(this) instead of switching on error types themselves.
public static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(
        this ErrorOr<T> result,
        ControllerBase controller)
    {
        if (!result.IsError)
        {
            return controller.Ok(result.Value);
        }

        // The first error's Type decides the status code; the full error list is still returned in the body.
        var firstError = result.Errors.First();

        return firstError.Type switch
        {
            ErrorType.Validation => controller.BadRequest(result.Errors),
            ErrorType.NotFound => controller.NotFound(result.Errors),
            ErrorType.Conflict => controller.Conflict(result.Errors),
            ErrorType.Unauthorized => controller.Unauthorized(result.Errors),
            // Forbid() would return an empty body (it triggers the auth scheme's forbid handler) — use StatusCode so the error payload is preserved.
            ErrorType.Forbidden => controller.StatusCode(StatusCodes.Status403Forbidden, result.Errors),
            _ => controller.StatusCode(StatusCodes.Status500InternalServerError, result.Errors)
        };
    }
}
