public sealed class ExceptionHandlingOptions
{
    /// <summary>Nom de la section dans appsettings.json</summary>
    public const string SectionName = "ExceptionHandling";

    /// <summary>
    /// Active la persistance des exceptions en base de données.
    /// Défaut : <c>true</c>.
    /// </summary>
    public bool EnableDatabaseLogging { get; set; } = true;

    /// <summary>
    /// Active l'envoi d'un mail d'alerte lors d'une exception non gérée.
    /// Défaut : <c>true</c>.
    /// </summary>
    public bool EnableMailNotification { get; set; } = true;

    /// <summary>
    /// Liste des noms de types d'exceptions à ignorer (non loggées, non notifiées).
    /// Exemple : <c>["OperationCanceledException", "TaskCanceledException"]</c>.
    /// </summary>
    public List<string> IgnoredExceptionTypes { get; set; } = [];

    /// <summary>
    /// Message générique retourné au client en cas d'erreur non gérée.
    /// Ne doit jamais contenir de détails techniques (Checkmarx).
    /// </summary>
    public string UserFriendlyMessage { get; set; } =
        "Une erreur inattendue s'est produite. Veuillez réessayer ou contacter le support.";
}
