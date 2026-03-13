namespace Catalog.Api.Middleware;

using System.Text.Json;
using Catalog.Api.Common;
using FluentValidation;
using Microsoft.Extensions.Logging;

/// <summary>
/// Global exception handler.
/// - User (HTTP response): simple, human-readable message + TraceId for support.
/// - Developer (logs): full details - exception type, message, stack trace, TraceId.
/// </summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ctx, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext ctx, Exception ex)
    {
        var traceId    = ctx.TraceIdentifier;
        var statusCode = GetStatusCode(ex);

        // Developer logs: full details (TraceId, exception type, message, stacktrace)
        logger.LogError(
            ex,
            "[ERROR] TraceId={TraceId} | Status={StatusCode} | Type={ExceptionType} | Message={ExceptionMessage}",
            traceId,
            statusCode,
            ex.GetType().FullName,
            ex.Message);

        // User response: simple, readable
        var response = BuildUserResponse(ex, traceId, statusCode);

        ctx.Response.ContentType = "application/json";
        ctx.Response.StatusCode  = statusCode;

        await ctx.Response.WriteAsync(JsonSerializer.Serialize(response, _jsonOptions));
    }

    private static int GetStatusCode(Exception ex) => ex switch
    {
        ValidationException         => StatusCodes.Status400BadRequest,
        KeyNotFoundException        => StatusCodes.Status404NotFound,
        ArgumentException           => StatusCodes.Status400BadRequest,
        UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
        _                           => StatusCodes.Status500InternalServerError
    };

    /// <summary>
    /// Returns a user-friendly ApiResponse.
    /// Validation errors expose field-level detail (useful for forms).
    /// All other errors use a generic message - no internal details leaked.
    /// </summary>
    private static ApiResponse<object> BuildUserResponse(Exception ex, string traceId, int statusCode)
    {
        if (ex is ValidationException validationEx)
        {
            var errors = validationEx.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            return new ApiResponse<object>
            {
                Success          = false,
                TraceId          = traceId,
                Message          = "Les donnees saisies sont invalides. Veuillez corriger les erreurs.",
                ValidationErrors = errors
            };
        }

        var userMessage = statusCode switch
        {
            StatusCodes.Status404NotFound     => "La ressource demandee est introuvable.",
            StatusCodes.Status401Unauthorized  => "Acces non autorise. Veuillez vous connecter.",
            StatusCodes.Status400BadRequest    => "La requete est invalide.",
            _                                  => "Une erreur inattendue est survenue. Veuillez reessayer ulterieurement."
        };

        return new ApiResponse<object>
        {
            Success = false,
            TraceId = traceId,
            Message = userMessage
        };
    }
}