using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly ExceptionHandlingOptions _options;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IOptions<ExceptionHandlingOptions> options)
    {
        _next    = next;
        _logger  = logger;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // Ignorer les types d'exceptions configurés (ex: OperationCanceledException)
            if (_options.IgnoredExceptionTypes.Contains(ex.GetType().Name))
            {
                throw;
            }

            await HandleExceptionAsync(context, ex);
        }
    }

    // ── Gestion de l'exception ────────────────────────────────────────────────

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Extraction du CPRJ : depuis le claim JWT en priorité, sinon depuis le header
        var cprj = context.User?.FindFirst("cprj")?.Value
                ?? context.Request.Headers["X-Cprj"].FirstOrDefault()
                ?? "UNKNOWN";

        var username = context.User?.FindFirst("couibp")?.Value
                    ?? context.User?.Identity?.Name
                    ?? "Anonymous";

        // 1. Log structuré (Serilog / ILogger)
        _logger.LogError(
            exception,
            "Exception non gérée | CPRJ: {Cprj} | User: {Username} | Method: {Method} | Path: {Path}",
            cprj,
            username,
            context.Request.Method,
            context.Request.Path);

        // 2. Persistance en base de données (si activée)
        if (_options.EnableDatabaseLogging)
        {
            await PersistExceptionAsync(context, exception, cprj, username);
        }

        // 3. Notification mail — fire and forget sécurisé (non bloquant)
        if (_options.EnableMailNotification)
        {
            // On capture les services avant la fin du scope de la requête
            var mailService = context.RequestServices
                .GetRequiredService<IExceptionMailService>();

            _ = Task.Run(async () =>
            {
                try
                {
                    await mailService.SendAlertAsync(exception, cprj, username);
                }
                catch (Exception mailEx)
                {
                    _logger.LogError(
                        mailEx,
                        "Échec de l'envoi du mail d'alerte | CPRJ: {Cprj}",
                        cprj);
                }
            });
        }

        // 4. Réponse user-friendly — jamais de stack trace exposée (Checkmarx / Sonar)
        await WriteUserFriendlyResponseAsync(context);
    }

    // ── Persistance DB ────────────────────────────────────────────────────────

    private async Task PersistExceptionAsync(
        HttpContext context,
        Exception exception,
        string cprj,
        string username)
    {
        try
        {
            var logRepository = context.RequestServices
                .GetRequiredService<IExceptionLogRepository>();

            var exceptionLog = new ExceptionLog
            {
                Message    = exception.Message,
                StackTrace = exception.StackTrace,
                Path       = context.Request.Path.Value,
                HttpMethod = context.Request.Method,
                StatusCode = StatusCodes.Status500InternalServerError,
                Username   = username,
                Cprj       = cprj,
                OccurredAt = DateTime.UtcNow
            };

            await logRepository.SaveAsync(exceptionLog);
        }
        catch (Exception dbEx)
        {
            // Ne jamais laisser l'échec du logger bloquer la réponse au client
            _logger.LogCritical(
                dbEx,
                "Échec critique de la persistance de l'exception en base de données | CPRJ: {Cprj}",
                cprj);
        }
    }

    // ── Réponse client ────────────────────────────────────────────────────────

    private async Task WriteUserFriendlyResponseAsync(HttpContext context)
    {
        // Vérifier que la réponse n'a pas déjà commencé à être envoyée
        if (context.Response.HasStarted)
        {
            _logger.LogWarning(
                "Impossible d'écrire la réponse d'erreur : la réponse HTTP a déjà démarré.");
            return;
        }

        context.Response.StatusCode  = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        var response = new
        {
            success = false,
            // Message configurable — jamais de détail technique (Checkmarx)
            message = _options.UserFriendlyMessage,
            // TraceId pour corrélation avec les logs et le support technique
            traceId = context.TraceIdentifier
        };

        var json = JsonSerializer.Serialize(response, JsonOptions);
        await context.Response.WriteAsync(json);
    }
