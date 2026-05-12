using ErrorOr;
using Microsoft.AspNetCore.Mvc;

namespace CineMatch.API.Common;

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

            var firstError = result.Errors.First();

            return firstError.Type switch
            {
                ErrorType.Validation => controller.BadRequest(result.Errors),
                ErrorType.NotFound => controller.NotFound(result.Errors),
                ErrorType.Conflict => controller.Conflict(result.Errors),
                ErrorType.Unauthorized => controller.Unauthorized(result.Errors),
                ErrorType.Forbidden => controller.StatusCode(StatusCodes.Status403Forbidden, result.Errors),
                _ => controller.StatusCode(500, result.Errors)
            };
        }
}

