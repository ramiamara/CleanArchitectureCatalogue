namespace BPRI.ExceptionHandling;

/// <summary>Enregistre une exception sans passer par une requête HTTP (batch, service Windows, application console).</summary>
public interface IExceptionReporter
{
    /// <summary>
    /// Écrit l'exception comme le ferait le middleware : fichier, SQL Server et e-mail (niveau Error), avec type, empreinte et contexte.
    /// Le niveau suit le statut : 404 Information, autres 4xx Warning, 5xx Error.
    /// </summary>
    void Report(Exception exception, int statusCode = 500);
}

internal sealed class ExceptionReporter : IExceptionReporter
{
    private readonly ExceptionLogger _logger;

    public ExceptionReporter(ExceptionLogger logger)
    {
        _logger = logger;
    }

    public void Report(Exception exception, int statusCode = 500)
    {
        _logger.Write(ExceptionLogger.LevelFor(statusCode), exception, statusCode);
    }
}
