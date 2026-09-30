public interface IExceptionLogRepository
{
    /// <summary>
    /// Persiste une entrée de log d'exception dans la table <c>ExceptionLogs</c>.
    /// </summary>
    /// <param name="exceptionLog">L'entité à persister.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    Task SaveAsync(
        ExceptionLog exceptionLog,
        CancellationToken cancellationToken = default);
}
