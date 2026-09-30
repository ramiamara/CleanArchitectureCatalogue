using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Polly;
using Polly.Retry;
internal sealed class ExceptionMailService : IExceptionMailService
{
    private readonly ExceptionMailOptions _mailOptions;
    private readonly ILogger<ExceptionMailService> _logger;
    private readonly MailTemplateLoader _templateLoader;
    private readonly AsyncRetryPolicy _retryPolicy;

    public ExceptionMailService(
        IOptions<ExceptionMailOptions> mailOptions,
        ILogger<ExceptionMailService> logger,
        MailTemplateLoader templateLoader)
    {
        _mailOptions    = mailOptions.Value;
        _logger         = logger;
        _templateLoader = templateLoader;

        // Polly : retry avec backoff exponentiel configuré depuis les options
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: _mailOptions.MaxRetryAttempts,
                sleepDurationProvider: attempt =>
                    TimeSpan.FromSeconds(Math.Pow(_mailOptions.RetryDelaySeconds, attempt)),
                onRetry: (exception, delay, attempt, _) =>
                {
                    _logger.LogWarning(
                        "Tentative {Attempt}/{Max} d'envoi du mail d'alerte échouée. " +
                        "Nouvelle tentative dans {Delay}s. Erreur : {Error}",
                        attempt,
                        _mailOptions.MaxRetryAttempts,
                        delay.TotalSeconds,
                        exception.Message);
                });
    }

    /// <inheritdoc/>
    public async Task SendAlertAsync(
        Exception exception,
        string cprj,
        string username,
        CancellationToken cancellationToken = default)
    {
        // Chargement du template (depuis cache si déjà chargé)
        var template = await _templateLoader.LoadAsync(cancellationToken);

        // Application des placeholders — encodage HTML intégré dans ApplyPlaceholders
        var htmlBody = MailTemplateLoader.ApplyPlaceholders(
            template:      template,
            cprj:          cprj,
            username:      username,
            exceptionType: exception.GetType().FullName ?? exception.GetType().Name,
            message:       exception.Message,
            stackTrace:    exception.StackTrace ?? "N/A",
            dateUtc:       DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));

        await _retryPolicy.ExecuteAsync(async () =>
        {
            using var client = new SmtpClient();

            var secureSocketOptions = _mailOptions.UseSsl
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.None;

            await client.ConnectAsync(
                _mailOptions.SmtpHost,
                _mailOptions.SmtpPort,
                secureSocketOptions,
                cancellationToken);

            // Authentification SMTP si des credentials sont fournis
            if (!string.IsNullOrWhiteSpace(_mailOptions.SmtpUser)
                && !string.IsNullOrWhiteSpace(_mailOptions.SmtpPassword))
            {
                await client.AuthenticateAsync(
                    _mailOptions.SmtpUser,
                    _mailOptions.SmtpPassword,
                    cancellationToken);
            }

            var message = BuildMailMessage(exception, cprj, htmlBody);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(quit: true, cancellationToken);
        });
    }

    // ── Méthodes privées ──────────────────────────────────────────────────────

    /// <summary>
    /// Construit le message MimeKit avec le corps HTML déjà résolu depuis le template.
    /// </summary>
    private MimeMessage BuildMailMessage(Exception exception, string cprj, string htmlBody)
    {
        var message = new MimeMessage();

        message.From.Add(new MailboxAddress(_mailOptions.FromName, _mailOptions.FromAddress));

        foreach (var address in _mailOptions.ToAddresses)
        {
            message.To.Add(MailboxAddress.Parse(address));
        }

        message.Subject = $"{_mailOptions.SubjectPrefix} [{cprj}] {exception.GetType().Name}";

        var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
        message.Body = bodyBuilder.ToMessageBody();

        return message;
    }
}
