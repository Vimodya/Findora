using Microsoft.AspNetCore.Mvc;

namespace Findora.API.Middleware;

/// <summary>
/// Catches any unhandled exception from later middleware/controllers and
/// converts it into a consistent <see cref="ProblemDetails"/> JSON response,
/// instead of letting a raw stack trace or exception message reach the
/// client. Register first in the pipeline (see <c>Program.cs</c>) so it
/// wraps every other middleware, controller action, and future endpoint.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            // Only method/path are logged here — never headers, query
            // strings, or request bodies, which could carry passwords,
            // tokens, or other sensitive data.
            _logger.LogError(
                exception,
                "Unhandled exception while processing {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            await WriteProblemDetailsAsync(context, exception);
        }
    }

    private static Task WriteProblemDetailsAsync(HttpContext context, Exception exception)
    {
        // If the response has already started, there's nothing we can do —
        // rethrow so the server's default handling takes over.
        if (context.Response.HasStarted)
        {
            throw exception;
        }

        var statusCode = MapStatusCode(exception);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = ReasonPhraseFor(statusCode),
            // Intentionally generic — never the exception message or stack
            // trace, which could leak internal implementation details.
            Detail = "An unexpected error occurred while processing your request.",
            Instance = context.Request.Path
        };
        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.Clear();
        context.Response.StatusCode = statusCode;

        // WriteAsJsonAsync overwrites Content-Type with its own default
        // unless told otherwise, so the desired media type must be passed
        // explicitly here rather than set on the response beforehand.
        return context.Response.WriteAsJsonAsync(
            problemDetails,
            options: (System.Text.Json.JsonSerializerOptions?)null,
            contentType: "application/problem+json");
    }

    /// <summary>
    /// Maps a handful of common exception types to a more specific HTTP
    /// status code; anything else falls back to 500. Kept intentionally
    /// small — richer, domain-specific mapping belongs in later modules
    /// (e.g. validation errors in Module 4+, auth errors in Module 2).
    /// </summary>
    private static int MapStatusCode(Exception exception) => exception switch
    {
        ArgumentException => StatusCodes.Status400BadRequest,
        UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
        KeyNotFoundException => StatusCodes.Status404NotFound,
        NotImplementedException => StatusCodes.Status501NotImplemented,
        _ => StatusCodes.Status500InternalServerError
    };

    private static string ReasonPhraseFor(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "Bad Request",
        StatusCodes.Status401Unauthorized => "Unauthorized",
        StatusCodes.Status404NotFound => "Not Found",
        StatusCodes.Status501NotImplemented => "Not Implemented",
        _ => "An unexpected error occurred."
    };
}

/// <summary>
/// Registration helper so <c>Program.cs</c> reads cleanly:
/// <c>app.UseFindoraExceptionHandling();</c>
/// </summary>
public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseFindoraExceptionHandling(this IApplicationBuilder app)
        => app.UseMiddleware<ExceptionHandlingMiddleware>();
}
