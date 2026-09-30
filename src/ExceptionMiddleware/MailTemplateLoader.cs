using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
/// <summary>
/// Service de chargement du template HTML pour le mail d'alerte d'exception.
/// Supporte trois modes de chargement selon la valeur de <see cref="ExceptionMailOptions.TemplateUrl"/> :
/// <list type="bullet">
///   <item>URL HTTP/HTTPS : téléchargement via <see cref="HttpClient"/></item>
///   <item>Chemin fichier local (absolu ou relatif) : lecture depuis le système de fichiers</item>
///   <item>Non configuré ou inaccessible : template de secours intégré utilisé</item>
/// </list>
/// Le template est chargé une seule fois au démarrage et mis en cache (Singleton).
/// </summary>
internal sealed class MailTemplateLoader
{
    private readonly ExceptionMailOptions _options;
    private readonly ILogger<MailTemplateLoader> _logger;
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Template chargé et mis en cache au premier appel de <see cref="LoadAsync"/>.
    /// </summary>
    private string? _cachedTemplate;

    /// <summary>
    /// Placeholders reconnus dans le template HTML.
    /// </summary>
    internal static class Placeholders
    {
        public const string Cprj          = "{{CPRJ}}";
        public const string Username      = "{{USERNAME}}";
        public const string ExceptionType = "{{EXCEPTION_TYPE}}";
        public const string Message       = "{{MESSAGE}}";
        public const string StackTrace    = "{{STACKTRACE}}";
        public const string DateUtc       = "{{DATE_UTC}}";
    }

    public MailTemplateLoader(
        IOptions<ExceptionMailOptions> options,
        ILogger<MailTemplateLoader> logger,
        HttpClient httpClient)
    {
        _options    = options.Value;
        _logger     = logger;
        _httpClient = httpClient;
    }

    /// <summary>
    /// Charge le template HTML depuis la source configurée (cache après le premier appel).
    /// En cas d'échec, retourne le template de secours intégré.
    /// </summary>
    public async Task<string> LoadAsync(CancellationToken cancellationToken = default)
    {
        // Retourner le cache si déjà chargé
        if (_cachedTemplate is not null)
        {
            return _cachedTemplate;
        }

        _cachedTemplate = await LoadFromSourceAsync(cancellationToken);
        return _cachedTemplate;
    }

    /// <summary>
    /// Applique les valeurs sur le template en remplaçant les placeholders.
    /// Toutes les valeurs sont encodées HTML pour éviter les injections (Checkmarx).
    /// </summary>
    public static string ApplyPlaceholders(
        string template,
        string cprj,
        string username,
        string exceptionType,
        string message,
        string stackTrace,
        string dateUtc)
    {
        return template
            .Replace(Placeholders.Cprj,          Encode(cprj),          StringComparison.Ordinal)
            .Replace(Placeholders.Username,       Encode(username),      StringComparison.Ordinal)
            .Replace(Placeholders.ExceptionType,  Encode(exceptionType), StringComparison.Ordinal)
            .Replace(Placeholders.Message,        Encode(message),       StringComparison.Ordinal)
            .Replace(Placeholders.StackTrace,     Encode(stackTrace),    StringComparison.Ordinal)
            .Replace(Placeholders.DateUtc,        Encode(dateUtc),       StringComparison.Ordinal);
    }

    // ── Méthodes privées ──────────────────────────────────────────────────────

    private async Task<string> LoadFromSourceAsync(CancellationToken cancellationToken)
    {
        var templateUrl = _options.TemplateUrl;

        if (string.IsNullOrWhiteSpace(templateUrl))
        {
            _logger.LogInformation(
                "TemplateUrl non configuré. Utilisation du template de secours intégré.");
            return GetFallbackTemplate();
        }

        try
        {
            // Chargement depuis une URL HTTP/HTTPS
            if (templateUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                templateUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return await LoadFromHttpAsync(templateUrl, cancellationToken);
            }

            // Chargement depuis un fichier local (absolu ou relatif)
            return await LoadFromFileAsync(templateUrl, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Échec du chargement du template depuis '{TemplateUrl}'. " +
                "Utilisation du template de secours intégré.",
                templateUrl);

            return GetFallbackTemplate();
        }
    }

    private async Task<string> LoadFromHttpAsync(
        string url,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Chargement du template mail depuis l'URL : {Url}", url);

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        _logger.LogInformation(
            "Template mail chargé avec succès depuis l'URL : {Url}", url);

        return content;
    }

    private async Task<string> LoadFromFileAsync(
        string path,
        CancellationToken cancellationToken)
    {
        // Résolution du chemin relatif par rapport à la racine de l'application
        var fullPath = Path.IsPathRooted(path)
            ? path
            : Path.Combine(AppContext.BaseDirectory, path);

        _logger.LogInformation(
            "Chargement du template mail depuis le fichier : {Path}", fullPath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                $"Le fichier template '{fullPath}' est introuvable.", fullPath);
        }

        var content = await File.ReadAllTextAsync(fullPath, cancellationToken);

        _logger.LogInformation(
            "Template mail chargé avec succès depuis le fichier : {Path}", fullPath);

        return content;
    }

    /// <summary>
    /// Template de secours minimal utilisé si le template configuré est inaccessible.
    /// Garantit qu'un mail est toujours envoyé même en cas de problème de configuration.
    /// </summary>
    private static string GetFallbackTemplate() =>
        """
        <!DOCTYPE html>
        <html lang="fr">
        <head><meta charset="UTF-8"/></head>
        <body style="font-family:Arial,sans-serif;font-size:14px;color:#333;">
            <h2 style="color:#c0392b;">&#9888; Exception non g&eacute;r&eacute;e</h2>
            <table style="border-collapse:collapse;width:100%;">
                <tr><td style="padding:8px;border:1px solid #ddd;font-weight:bold;width:160px;">CPRJ</td>
                    <td style="padding:8px;border:1px solid #ddd;">{{CPRJ}}</td></tr>
                <tr><td style="padding:8px;border:1px solid #ddd;font-weight:bold;">Utilisateur</td>
                    <td style="padding:8px;border:1px solid #ddd;">{{USERNAME}}</td></tr>
                <tr><td style="padding:8px;border:1px solid #ddd;font-weight:bold;">Type</td>
                    <td style="padding:8px;border:1px solid #ddd;">{{EXCEPTION_TYPE}}</td></tr>
                <tr><td style="padding:8px;border:1px solid #ddd;font-weight:bold;">Message</td>
                    <td style="padding:8px;border:1px solid #ddd;">{{MESSAGE}}</td></tr>
                <tr><td style="padding:8px;border:1px solid #ddd;font-weight:bold;">Date (UTC)</td>
                    <td style="padding:8px;border:1px solid #ddd;">{{DATE_UTC}}</td></tr>
            </table>
            <h3>Stack Trace</h3>
            <pre style="background:#f4f4f4;padding:12px;border-radius:4px;font-size:12px;white-space:pre-wrap;">{{STACKTRACE}}</pre>
        </body>
        </html>
        """;

    /// <summary>
    /// Encodage HTML pour éviter les injections dans le corps du mail (Checkmarx).
    /// </summary>
    private static string Encode(string value)
        => System.Net.WebUtility.HtmlEncode(value);
}
