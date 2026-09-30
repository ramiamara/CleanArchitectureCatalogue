public sealed class ExceptionMailOptions
{
    /// <summary>Nom de la section dans appsettings.json</summary>
    public const string SectionName = "ExceptionMail";

    /// <summary>Hôte du serveur SMTP (ex : smtp.intranet.local)</summary>
    public required string SmtpHost { get; set; }

    /// <summary>Port SMTP. Défaut : 587</summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>Active SSL/TLS. Défaut : true</summary>
    public bool UseSsl { get; set; } = true;

    /// <summary>Identifiant SMTP (optionnel si authentification Windows)</summary>
    public string? SmtpUser { get; set; }

    /// <summary>Mot de passe SMTP (optionnel)</summary>
    public string? SmtpPassword { get; set; }

    /// <summary>Adresse email expéditrice</summary>
    public required string FromAddress { get; set; }

    /// <summary>Nom affiché de l'expéditeur</summary>
    public required string FromName { get; set; }

    /// <summary>Liste des adresses destinataires des alertes</summary>
    public required List<string> ToAddresses { get; set; }

    /// <summary>
    /// Préfixe du sujet du mail d'alerte.
    /// Défaut : <c>[BPRI][ERREUR]</c>
    /// </summary>
    public string SubjectPrefix { get; set; } = "[BPRI][ERREUR]";

    /// <summary>
    /// Nombre maximum de tentatives d'envoi (Polly retry).
    /// Défaut : 3
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Délai initial entre les tentatives en secondes (backoff exponentiel).
    /// Défaut : 2
    /// </summary>
    public int RetryDelaySeconds { get; set; } = 2;

    /// <summary>
    /// URL ou chemin du fichier template HTML utilisé pour le corps du mail d'alerte.
    /// Peut être :
    /// <list type="bullet">
    ///   <item>Un chemin local absolu : <c>C:\templates\exception-mail.html</c></item>
    ///   <item>Un chemin relatif à la racine de l'application : <c>Templates/exception-mail.html</c></item>
    ///   <item>Une URL HTTP/HTTPS : <c>https://cdn.intranet.local/templates/exception-mail.html</c></item>
    /// </list>
    /// Le template doit contenir les placeholders suivants :
    /// <c>{{CPRJ}}</c>, <c>{{USERNAME}}</c>, <c>{{EXCEPTION_TYPE}}</c>,
    /// <c>{{MESSAGE}}</c>, <c>{{STACKTRACE}}</c>, <c>{{DATE_UTC}}</c>.
    /// Si non renseigné ou inaccessible, un template de secours intégré est utilisé.
    /// </summary>
    public string? TemplateUrl { get; set; }
}
