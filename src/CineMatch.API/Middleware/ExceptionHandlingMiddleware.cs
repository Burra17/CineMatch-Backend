using CineMatch.API.Contracts;
using System.Net;
using System.Text.Json;

namespace CineMatch.API.Middleware;

// Safety net for unexpected exceptions. Expected business errors (not found, conflict, etc.)
// go through ErrorOr and ResultExtensions — anything that reaches here is a bug or system failure.
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title) = MapException(exception);

        _logger.LogError(
            exception,
            "Unhandled exception occurred. Path: {Path}, Method: {Method}, TraceId: {TraceId}",
            context.Request.Path,
            context.Request.Method,
            context.TraceIdentifier);

        var response = new ErrorResponse
        {
            Status = statusCode,
            Title = title,
            TraceId = context.TraceIdentifier,
            // Only leak the exception message in Development — production clients get a generic title to avoid exposing internals.
            Detail = _environment.IsDevelopment() ? exception.Message : null
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
    }

    // Seam for mapping specific exception types to status codes if needed later.
    // Today everything is 500 — add cases here (e.g. OperationCanceledException) when a real need shows up.
    private static (int StatusCode, string Title) MapException(Exception exception)
    {
        return exception switch
        {
            _ => ((int)HttpStatusCode.InternalServerError, "An unexpected error occurred"),
        };
    }
}
