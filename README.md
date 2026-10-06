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

---

## Logs

Les logs sont ecrits dans :
- **Console** (format textuel couleur)
- **`src/Catalog.Api/logs/api-YYYYMMDD.txt`** (fichier rotatif quotidien)

Niveau minimum : `Debug` (configurable via `appsettings.json`).

---

## Pagination

Les endpoints de liste supportent la pagination :

```
GET /api/catalogues?page=1&pageSize=10
GET /api/products?page=1&pageSize=20
```
-- Base des logs (adapter le nom si besoin)
USE BpriLogsDb;
GO

IF OBJECT_ID(N'dbo.RequestLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RequestLogs
    (
        Id              INT IDENTITY(1,1) NOT NULL,
        Message         NVARCHAR(MAX)  NULL,
        [Level]         NVARCHAR(128)  NULL,
        [TimeStamp]     DATETIME       NOT NULL,   -- UTC
        TraceId         NVARCHAR(64)   NULL,
        Cprj            NVARCHAR(64)   NULL,
        ApplicationName NVARCHAR(128)  NULL,
        EnvironmentName NVARCHAR(64)   NULL,
        MachineName     NVARCHAR(128)  NULL,
        HttpMethod      NVARCHAR(16)   NULL,
        [Path]          NVARCHAR(512)  NULL,
        QueryString     NVARCHAR(2048) NULL,
        StatusCode      INT            NULL,
        DurationMs      INT            NULL,
        UserName        NVARCHAR(256)  NULL,
        UserId          NVARCHAR(128)  NULL,
        RequestBody     NVARCHAR(MAX)  NULL,
        ResponseBody    NVARCHAR(MAX)  NULL,
        CONSTRAINT PK_RequestLogs PRIMARY KEY CLUSTERED (Id)
    );

    -- Index utiles pour la plateforme de logs (filtre par projet, application, période, TraceId)
    CREATE NONCLUSTERED INDEX IX_RequestLogs_Cprj_TimeStamp
        ON dbo.RequestLogs (Cprj, [TimeStamp]) INCLUDE (ApplicationName, StatusCode, DurationMs);

    CREATE NONCLUSTERED INDEX IX_RequestLogs_TraceId
        ON dbo.RequestLogs (TraceId);
END
GO
Reponse : `PagedResult<T>` avec `Items`, `TotalCount`, `Page`, `PageSize`.
