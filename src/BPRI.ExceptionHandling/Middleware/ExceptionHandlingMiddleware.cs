using Microsoft.AspNetCore.Http;

namespace BPRI.ExceptionHandling;

internal sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ExceptionLogger _logger;
    private readonly ExceptionHandlingOptions _options;

    public ExceptionHandlingMiddleware(RequestDelegate next, ExceptionLogger logger, ExceptionHandlingOptions options)
    {
        _next = next;
        _logger = logger;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string traceId = RequestContextEnricher.TraceId(context) ?? "";
        context.Response.Headers[_options.TraceIdHeader] = traceId;

        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Client déconnecté : rien à logger.
            context.Response.StatusCode = 499;
        }
        catch (Exception ex)
        {
            ErrorInfo error = _options.MapException?.Invoke(ex) ?? MapDefault(ex);

            _logger.Write(ExceptionLogger.LevelFor(error.StatusCode), ex, error.StatusCode);

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.StatusCode = error.StatusCode;
            if (_options.WriteResponse != null)
            {
                await _options.WriteResponse(context, error, traceId);
            }
            else
            {
                await WriteProblem(context, error, traceId);
            }
        }
    }

    private ErrorInfo MapDefault(Exception ex)
    {
        if (ex is AppException appException)
        {
            return new ErrorInfo(appException.StatusCode, appException.Message);
        }
        if (ex is KeyNotFoundException)
        {
            return new ErrorInfo(404, "La ressource demandée est introuvable.");
        }
        if (ex is UnauthorizedAccessException)
        {
            return new ErrorInfo(401, "Accès non autorisé.");
        }
        if (ex is ArgumentException || ex is FormatException)
        {
            return new ErrorInfo(400, "La requête est invalide.");
        }
        return new ErrorInfo(500, _options.UserFriendlyMessage);
    }

    private static Task WriteProblem(HttpContext context, ErrorInfo error, string traceId)
    {
        var extensions = new Dictionary<string, object?>();
        extensions["traceId"] = traceId;
        if (error.Errors != null)
        {
            extensions["errors"] = error.Errors;
        }

        IResult problem = Results.Problem(title: error.Message, statusCode: error.StatusCode, extensions: extensions);
        return problem.ExecuteAsync(context);
    }
}
