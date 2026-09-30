public interface IExceptionMailService
{
    /// <summary>
    /// Envoie un mail d'alerte aux destinataires configurés.
    /// L'envoi est effectué avec retry automatique (Polly).
    /// </summary>
    /// <param name="exception">L'exception à notifier.</param>
    /// <param name="cprj">Code projet source de l'exception.</param>
    /// <param name="username">Login de l'utilisateur connecté au moment de l'exception.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    Task SendAlertAsync(
        Exception exception,
        string cprj,
        string username,
        CancellationToken cancellationToken = default);
}
