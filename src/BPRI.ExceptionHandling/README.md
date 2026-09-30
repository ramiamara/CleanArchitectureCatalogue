# BPRI.ExceptionHandling

Un middleware ASP.NET Core (.NET 9) qui attrape les exceptions, répond au client par un message simple + un `TraceId`, et journalise avec **Serilog** en ajoutant le contexte du projet appelant (**cprj**, utilisateur, claims JWT).

| Destination | Sink Serilog | Contenu | Activation |
|---|---|---|---|
| Fichier texte ou JSON | `Serilog.Sinks.File` | exceptions du middleware | `ExceptionHandling:File` |
| SQL Server | `Serilog.Sinks.MSSqlServer` | exceptions **et** logs applicatifs (`ILogger`) | `ExceptionHandling:Database` |
| E-mail (erreurs 500, regroupées) | `Serilog.Sinks.Email` | exceptions du middleware | `ExceptionHandling:Email` |

Le logger est propre à la bibliothèque : il ne remplace pas le logger de l'application.

## Utilisation

```csharp
builder.Services.AddBpriExceptionHandling(builder.Configuration);
// ...
app.UseBpriExceptionHandling();   // en premier dans le pipeline
```

## Configuration (appsettings.json)

```json
"ExceptionHandling": {
  "ApplicationName": "Catalog.Api",
  "Cprj": "CatalogueAPP",
  "UserFriendlyMessage": "Une erreur inattendue est survenue.",
  "Context": { "CprjClaim": "cprj", "CprjHeader": null, "UserIdClaim": "sub", "LoggedClaims": [ "role", "email" ] },
  "ApplicationLogs": { "Enabled": true, "MinimumLevel": "Information", "FrameworkMinimumLevel": "Warning" },
  "File": { "Enabled": true, "Path": "logs/exceptions-.log", "Json": false, "RetainedFileCount": 30 },
  "Database": {
    "Enabled": true,
    "ConnectionStringName": "ExceptionLogs",
    "TableName": "ExceptionLogs",
    "SchemaName": "dbo",
    "AutoCreateTable": true,
    "CreateDatabaseIfMissing": true
  },
  "Email": {
    "Enabled": false,
    "Host": "smtp.exemple.com", "Port": 587, "Security": "StartTls",
    "UserName": "", "Password": "",
    "From": "app@exemple.com", "To": [ "equipe@exemple.com" ],
    "SubjectPrefix": "[Exception] ", "MinimumLevel": "Error",
    "BatchPeriodSeconds": 30, "BatchSize": 20
  }
}
```

## Contexte enregistré avec chaque log

| Colonne | Source |
|---|---|
| `Cprj` | claim JWT `Context:CprjClaim` → en-tête `Context:CprjHeader` (désactivé par défaut, falsifiable) → `Cprj` → `ApplicationName` |
| `UserName`, `UserId` | `User.Identity.Name` / claim `Context:UserIdClaim` |
| `Claims` | JSON des seuls claims listés dans `Context:LoggedClaims` (jamais le jeton, jamais les autres claims) |
| `TraceId`, `HttpMethod`, `Path` | requête en cours |
| `ApplicationName`, `EnvironmentName`, `MachineName` | application |

Sécurité : n'ajouter dans `LoggedClaims` que des claims non sensibles. Ne jamais mettre de secret ou de données personnelles inutiles.

## Logs applicatifs

Avec `ApplicationLogs:Enabled`, tous les `ILogger` de l'application (niveau `MinimumLevel`, `Microsoft.*`/`System.*` au niveau `FrameworkMinimumLevel`) sont écrits dans la base avec le même contexte (`SourceContext` = catégorie). Ils ne vont ni dans le fichier ni par e-mail. Nécessite `Database:Enabled`.

## Comportement des exceptions

| Exception | HTTP | Niveau de log | Message au client |
|---|---|---|---|
| `AppException` | celui choisi | Warning (Information si 404) | son message |
| `KeyNotFoundException` | 404 | Information | ressource introuvable |
| `UnauthorizedAccessException` | 401 | Warning | accès non autorisé |
| `ArgumentException`, `FormatException` | 400 | Warning | requête invalide |
| toute autre | 500 | **Error** (e-mail) | `UserFriendlyMessage` |

Le `TraceId` est renvoyé dans le corps et dans l'en-tête `X-Trace-Id`.

## Personnaliser (optionnel)

```csharp
builder.Services.AddBpriExceptionHandling(builder.Configuration, o =>
{
    o.MapException  = ex => ex is MyException ? new ErrorInfo(409, "Conflit") : null;
    o.WriteResponse = (ctx, error, traceId) => ctx.Response.WriteAsJsonAsync(new { error.Message, traceId });
});
```

## Table SQL

Colonnes : `Id`, `Message`, `Level`, `TimeStamp` (UTC), `Exception`, `TraceId`, `Cprj`, `ApplicationName`, `EnvironmentName`, `MachineName`, `SourceContext`, `StatusCode`, `ExceptionType`, `InnerException`, `Fingerprint`, `HttpMethod`, `Path`, `UserName`, `UserId`, `Claims`.
Les colonnes `StatusCode`, `ExceptionType`, `InnerException`, `Fingerprint` sont vides pour les logs applicatifs.
Si une ancienne table `ExceptionLogs` existe (autre schéma), la supprimer : `DROP TABLE dbo.ExceptionLogs;`
Index conseillé : `CREATE INDEX IX_ExceptionLogs_Cprj_Time ON dbo.ExceptionLogs (Cprj, TimeStamp DESC);`

## Structure du projet

```
Exceptions/   AppException, ErrorInfo
Extensions/   ExceptionHandlerExtensions (AddBpriExceptionHandling, UseBpriExceptionHandling)
Middleware/   ExceptionHandlingMiddleware
Options/      ExceptionHandlingOptions (File, Database, Email, Context, ApplicationLogs)
Services/     ExceptionLogger, RequestContextEnricher, ApplicationLogProvider, HtmlEmailFormatter
Templates/    ErrorMail.html (modèle d'e-mail par défaut, remplaçable via Email:TemplatePath)
```
