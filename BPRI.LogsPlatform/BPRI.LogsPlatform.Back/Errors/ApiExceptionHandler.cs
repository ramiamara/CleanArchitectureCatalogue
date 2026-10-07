using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BPRI.LogsPlatform.Back.Errors;

/// <summary>Transforme les exceptions en réponse JSON (ProblemDetails). Les erreurs inattendues sont écrites dans le journal ASP.NET.</summary>
public sealed class ApiExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ApiExceptionHandler> _logger;

    public ApiExceptionHandler(ILogger<ApiExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        int status = StatusCodes.Status500InternalServerError;
        string title = "Une erreur est survenue.";

        if (exception is ApiException apiException)
        {
            status = apiException.StatusCode;
            title = apiException.Message;
        }
        else
        {
            _logger.LogError(exception, "Erreur non gérée sur {Path}", context.Request.Path);
        }

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ProblemDetails { Status = status, Title = title }, ct);
        return true;
    }
}
