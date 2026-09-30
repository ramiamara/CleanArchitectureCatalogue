using MailKit.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Events;

namespace BPRI.ExceptionHandling;

/// <summary>Configuration lue depuis la section <c>ExceptionHandling</c> de appsettings.</summary>
public sealed class ExceptionHandlingOptions
{
    public const string SectionName = "ExceptionHandling";

    /// <summary>Nom de l'application (défaut : nom de l'assembly hôte).</summary>
    public string? ApplicationName { get; set; }

    /// <summary>Cprj par défaut, utilisé quand aucun cprj n'est trouvé dans le JWT. Défaut : ApplicationName.</summary>
    public string? Cprj { get; set; }

    /// <summary>Message renvoyé au client pour une erreur 500.</summary>
    public string UserFriendlyMessage { get; set; } = "Une erreur inattendue est survenue. Veuillez réessayer ultérieurement.";

    /// <summary>En-tête de réponse contenant le TraceId.</summary>
    public string TraceIdHeader { get; set; } = "X-Trace-Id";

    /// <summary>Où lire le cprj et les informations de l'utilisateur (JWT).</summary>
    public ContextOptions Context { get; set; } = new();

    /// <summary>Enregistre aussi les logs ILogger de l'application dans la base.</summary>
    public ApplicationLogsOptions ApplicationLogs { get; set; } = new();

    public FileLogOptions File { get; set; } = new();
    public DatabaseLogOptions Database { get; set; } = new();
    public EmailLogOptions Email { get; set; } = new();

    /// <summary>(Code uniquement) Transforme une exception en réponse HTTP. Retourner null pour le comportement par défaut.</summary>
    public Func<Exception, ErrorInfo?>? MapException { get; set; }

    /// <summary>(Code uniquement) Écrit la réponse d'erreur (contexte, erreur, traceId). Défaut : ProblemDetails JSON.</summary>
    public Func<HttpContext, ErrorInfo, string, Task>? WriteResponse { get; set; }
}

/// <summary>Informations de contexte ajoutées à chaque log, lues dans le JWT de la requête en cours.</summary>
public sealed class ContextOptions
{
    /// <summary>Claim contenant le cprj de l'appelant.</summary>
    public string CprjClaim { get; set; } = "cprj";

    /// <summary>En-tête HTTP de secours pour le cprj quand il n'y a pas de JWT (désactivé par défaut : peut être falsifié par le client).</summary>
    public string? CprjHeader { get; set; }

    /// <summary>Claim contenant l'identifiant de l'utilisateur (sub par défaut).</summary>
    public string UserIdClaim { get; set; } = "sub";

    /// <summary>Autres claims à enregistrer (colonne Claims, au format JSON). Ex. : ["email", "role"]. Jamais le jeton lui-même.</summary>
    public List<string> LoggedClaims { get; set; } = [];
}

public sealed class ApplicationLogsOptions
{
    /// <summary>Nécessite Database:Enabled. Ces logs vont uniquement en base (pas dans le fichier ni par e-mail).</summary>
    public bool Enabled { get; set; }

    /// <summary>Niveau minimum pour le code de l'application.</summary>
    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;

    /// <summary>Niveau minimum pour les catégories Microsoft.* et System.* (très bavardes en Information).</summary>
    public LogLevel FrameworkMinimumLevel { get; set; } = LogLevel.Warning;
}

public sealed class FileLogOptions
{
    public bool Enabled { get; set; } = true;

    /// <summary>Chemin du fichier (relatif au dossier de l'application ou absolu). Un suffixe de date est ajouté avant l'extension.</summary>
    public string Path { get; set; } = "logs/exceptions-.log";

    /// <summary>Écrire en JSON (une ligne par log) plutôt qu'en texte.</summary>
    public bool Json { get; set; }

    public int RetainedFileCount { get; set; } = 30;
}

public sealed class DatabaseLogOptions
{
    public bool Enabled { get; set; }

    /// <summary>Chaîne de connexion directe. Si absente, on utilise ConnectionStrings:{ConnectionStringName}.</summary>
    public string? ConnectionString { get; set; }
    public string ConnectionStringName { get; set; } = "ExceptionLogs";

    public string TableName { get; set; } = "ExceptionLogs";
    public string SchemaName { get; set; } = "dbo";

    /// <summary>Crée la table au premier démarrage si elle n'existe pas.</summary>
    public bool AutoCreateTable { get; set; } = true;

    /// <summary>Crée aussi la base de données si elle n'existe pas (pratique en développement).</summary>
    public bool CreateDatabaseIfMissing { get; set; }
}

public sealed class EmailLogOptions
{
    public bool Enabled { get; set; }
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;

    /// <summary>None, Auto, SslOnConnect, StartTls, StartTlsWhenAvailable.</summary>
    public SecureSocketOptions Security { get; set; } = SecureSocketOptions.StartTls;

    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string From { get; set; } = "";
    public List<string> To { get; set; } = [];
    public string SubjectPrefix { get; set; } = "[Exception] ";

    /// <summary>Modèle HTML personnalisé (optionnel). Variables : {{Level}} {{Timestamp}} {{ApplicationName}} {{Cprj}} {{TraceId}} {{HttpMethod}} {{Path}} {{StatusCode}} {{ExceptionType}} {{User}} {{Environment}} {{Message}} {{Exception}} {{Color}}.</summary>
    public string? TemplatePath { get; set; }

    /// <summary>Niveau minimum déclenchant un e-mail (Error par défaut).</summary>
    public LogEventLevel MinimumLevel { get; set; } = LogEventLevel.Error;

    /// <summary>Les erreurs sont regroupées : un e-mail au plus toutes les N secondes (ou dès que BatchSize erreurs sont en attente).</summary>
    public int BatchPeriodSeconds { get; set; } = 30;
    public int BatchSize { get; set; } = 20;
}
