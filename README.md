# Catalogue API

API REST en **.NET 9** suivant les principes de la **Clean Architecture**. Elle permet de gérer des catalogues et leurs produits associes, avec authentification JWT, cache memoire, rate limiting et journalisation centralisee des exceptions.

---

## Architecture

```
CleanArchitectureCatalogue/
+-- src/
|   +-- Catalog.Api/            # Couche presentation (Minimal API + Carter)
|   +-- Catalog.Application/    # Logique metier (Services, DTOs, Validators)
|   +-- Catalog.Domain/         # Entites, ValueObjects, abstractions domaine
|   +-- Catalog.Infrastructure/ # EF Core, Repositories, Cache
|   +-- Catalog.Tests/          # Tests unitaires (xUnit + Moq)
+-- Catalog.sln
```

### Flux de dependances

```
Api --> Application --> Domain
Api --> Infrastructure --> Domain
```

> L'Infrastructure connait le Domain mais pas l'Application. L'Application connait le Domain mais pas l'Infrastructure. Le Domain ne connait personne.

---

## Stack technique

| Composant | Technologie |
|---|---|
| Framework | ASP.NET Core 9 — Minimal API |
| Routage | [Carter](https://github.com/CarterCommunity/Carter) |
| ORM | Entity Framework Core 9 (SQL Server) |
| Authentification | JWT Bearer (token externe AD/Identity Provider) |
| Validation | FluentValidation |
| Logging | Serilog (Console + fichier rotatif) |
| Cache | IMemoryCache (MemoryCacheService) |
| Rate Limiting | FixedWindow 200 req/min par IP |
| Gestion des exceptions | BPRI.ExceptionHandling (logs Serilog : fichier, SQL Server, e-mail) |
| Tests | xUnit + Moq + FluentAssertions |
| Documentation | Swagger / OpenAPI |
| CORS | Politique "AngularPolicy" (configurable) |

---

## Couches en detail

### `Catalog.Domain`
- **EntityBase** : classe de base avec audit automatique (`CreatedOn`, `CreatedBy`, `ModifiedOn`, `ModifiedBy`, `DeletedOn`, `DeletedBy`) et soft-delete (`IsDeleted`)
- **Entites** : `Catalogue`, `Product`
- **ValueObjects** : `Money` (montant + devise)
- Aucune dependance externe

### `Catalog.Application`
- **Services** : `ICatalogueService`, `IProductService` — logique metier pure
- **DTOs** : objets de transfert de donnees (request/response)
- **Validators** : regles FluentValidation par operation (Create, Update)
- Depends on : Domain, Infrastructure (via interfaces)

### `Catalog.Infrastructure`
- **Repository<T>** : generic repository avec filtre soft-delete automatique
- **UnitOfWork** : encapsule le `SaveChangesAsync` et expose les repositories
- **CatalogueContext** : DbContext EF Core avec configuration Fluent API
- **MemoryCacheService** : implementation ICacheService avec invalidation par prefixe
- **Migrations** : appliquees automatiquement au demarrage (`db.Database.Migrate()`)

### `Catalog.Api`
- **Endpoints** : `AuthEndpoints`, `CatalogueEndpoints`, `ProductEndpoints` (Carter ICarterModule)
- **Middleware** :
  - Gestion globale des erreurs : bibliotheque `BPRI.ExceptionHandling` (`src/BPRI.ExceptionHandling`, voir son README) — message simple + `traceId` pour l'utilisateur, details complets (stacktrace) dans les logs (`ILogger` + fichier `logs/exceptions-*.log`)
  - `JwtMiddleware` : enrichit le `HttpContext` avec les claims utilisateur
- **Common** : `ApiResponse<T>`, `ApiError`, `ExceptionHandlingSetup` (ValidationException → 400 et format d'erreur `ApiResponse`)

---

## Gestion des erreurs

| Exception | HTTP | Message utilisateur |
|---|---|---|
| `ValidationException` | 400 | "Les donnees saisies sont invalides" + champs |
| `ArgumentException` | 400 | "La requete est invalide" |
| `KeyNotFoundException` | 404 | "La ressource demandee est introuvable" |
| `UnauthorizedAccessException` | 401 | "Acces non autorise" |
| Toute autre exception | 500 | "Une erreur inattendue est survenue..." |

Le developpeur voit dans les logs : `TraceId | ExceptionType | Message | StackTrace`.

---

## Demarrage rapide

### Prerequis
- .NET 9 SDK
- SQL Server (local ou distant)

### Configuration

```jsonc
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=Catalogue;..."
  },
  "Jwt": {
    "SecretKey": "votre-cle-secrete-256-bits",
    "Issuer":    "votre-issuer",
    "Audience":  "votre-audience"
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:4200"]
  }
}
```

### Lancement

```powershell
dotnet run --project src\Catalog.Api\Catalog.Api.csproj --urls "http://localhost:5133"
```

| URL | Description |
|---|---|
| http://localhost:5133/swagger | Documentation interactive |
| http://localhost:5133/api/auth/login | Obtenir un token JWT |
| http://localhost:5133/api/catalogues | CRUD catalogues (authentifie) |
| http://localhost:5133/api/products | CRUD produits (authentifie) |

### Tests

```powershell
dotnet test src\Catalog.Tests\Catalog.Tests.csproj --verbosity normal
```

---

## Exceptions

Toute exception non geree est traitee par `BPRI.ExceptionHandling` : reponse JSON simple avec `traceId`, log fichier (`logs/exceptions-YYYYMMDD.log`) et enregistrement dans une **base dediee** `CatalogueLogsDb` (table `ExceptionLogs`, meme instance SQL Server, chaine `ConnectionStrings:ExceptionLogs`). La base et la table sont creees automatiquement en developpement ; en production, voir `src/BPRI.ExceptionHandling/README.md` (SQL et index conseillé).

  "ExceptionMail": {
    "SmtpHost": "smtp.l",
    "SmtpPort": 587,
    "UseSsl": true,
    "SmtpUser": "",
    "SmtpPassword": "",
    "FromAddress": "noreply@test.local",
    "FromName": "  Sample API",
    "ToAddresses": [
      "rami@test.local"
    ],
    "SubjectPrefix": "[BPR][CPRJ_A][ERREUR]",
    "MaxRetryAttempts": 3,
    "RetryDelaySeconds": 2,
    "TemplateUrl": "Templates/.html"
  },

## Logs

Les logs sont ecrits dans :
- **Console** (format textuel couleur)
- **`src/Catalog.Api/logs/api-YYYYMMDD.txt`** (fichier rotatif quotidien)

Niveau minimum : `Debug` (configurable via `appsettings.json`).

---

Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File(
                    formatter: new CompactJsonFormatter(),
                        //path: Path.Combine(Environment.CurrentDirectory, "appLog\\applog-.log"),
                        path: Path.Combine(exePath, "appLog\\applog-.log"),
                        //path: $"{exePath}\\appLog\\applog-.log",
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 3,
                        encoding: Encoding.GetEncoding("ISO-8859-1")
                    )
                .Enrich.WithThreadId()
                    .Enrich.WithProcessId()
                    .Enrich.WithAssemblyName()
                    .Enrich.WithUserId()
                    .Enrich.WithCorrelationId()
                    .CreateLogger();


using Serilog.Configuration;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Reflection;
using System.Diagnostics;
using System.Security.Principal;
using System.Web;

namespace DocuSignSvc.SerilogExtention
{
    public class SerilogEnrichers : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            LogEventProperty property = propertyFactory.CreateProperty("ThreadId", new ScalarValue(Environment.CurrentManagedThreadId));
            logEvent.AddPropertyIfAbsent(property);
        }
    }

    public class ProcessIdEnricher : ILogEventEnricher
    {
        private LogEventProperty _cachedProperty;

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            _cachedProperty = _cachedProperty ?? propertyFactory.CreateProperty("ProcessId", new ScalarValue(Process.GetCurrentProcess().Id));
            logEvent.AddPropertyIfAbsent(_cachedProperty);
        }
    }

    public class UserIdEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            string value = string.Empty;
            if (HttpRuntime.AppDomainAppId != null)
            {
                if (HttpContext.Current != null && HttpContext.Current.User != null && HttpContext.Current.User.Identity != null && !string.IsNullOrEmpty(HttpContext.Current.User.Identity.Name))
                {
                    value = HttpContext.Current.User.Identity.Name;
                }
            }
            else
            {
                value = WindowsIdentity.GetCurrent().Name;
            }

            if (string.IsNullOrEmpty(value))
            {
                value = "Anonymous";
            }

            LogEventProperty property = propertyFactory.CreateProperty("UserId", new ScalarValue(value));
            logEvent.AddPropertyIfAbsent(property);
        }
    }

    public class CorrelationIdEnricher : ILogEventEnricher
    {
        private static readonly string CorrelationIdItemName = $"{typeof(CorrelationIdEnricher).Name}+CorrelationId";

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            if (HttpContext.Current != null)
            {
                LogEventProperty property = propertyFactory.CreateProperty("CorrelationId", new ScalarValue(GetCorrelationId()));
                logEvent.AddPropertyIfAbsent(property);
            }
        }

        private static string GetCorrelationId()
        {
            return (string)(HttpContext.Current.Items[CorrelationIdItemName] ?? (HttpContext.Current.Items[CorrelationIdItemName] = Guid.NewGuid().ToString()));
        }
    }

}
et 
using Serilog.Configuration;
using Serilog.Events;
using Serilog;
using System;
using System.Reflection;

namespace DocuSignSvc.SerilogExtention
{
    public static class SerilogConfigurationExtensions
    {
        public static LoggerConfiguration WithThreadId(this LoggerEnrichmentConfiguration enrichmentConfiguration)
        {
            if (enrichmentConfiguration == null)
            {
                throw new ArgumentNullException("enrichmentConfiguration");
            }

            return enrichmentConfiguration.With<SerilogEnrichers>();
        }

        public static LoggerConfiguration WithProcessId(this LoggerEnrichmentConfiguration enrichmentConfiguration)
        {
            if (enrichmentConfiguration == null)
            {
                throw new ArgumentNullException("enrichmentConfiguration");
            }

            return enrichmentConfiguration.With<ProcessIdEnricher>();
        }

        public static LoggerConfiguration WithUserId(this LoggerEnrichmentConfiguration enrichmentConfiguration)
        {
            if (enrichmentConfiguration == null)
            {
                throw new ArgumentNullException("enrichmentConfiguration");
            }

            return enrichmentConfiguration.With<UserIdEnricher>();
        }

        public static LoggerConfiguration WithAssemblyName(this LoggerEnrichmentConfiguration enrichmentConfiguration)
        {
            if (enrichmentConfiguration == null)
            {
                throw new ArgumentNullException("enrichmentConfiguration");
            }

            Assembly assembly = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();
            return enrichmentConfiguration.WithProperty("AssemblyName", new ScalarValue(assembly.GetName().Name));
        }

        public static LoggerConfiguration WithCorrelationId(this LoggerEnrichmentConfiguration enrichmentConfiguration)
        {
            if (enrichmentConfiguration == null)
            {
                throw new ArgumentNullException("enrichmentConfiguration");
            }

            return enrichmentConfiguration.With<CorrelationIdEnricher>();
        }
    }
}

```

Reponse : `PagedResult<T>` avec `Items`, `TotalCount`, `Page`, `PageSize`.
