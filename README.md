# Catalogue API

API REST en **.NET 9** suivant les principes de la **Clean Architecture**. Elle permet de gérer des catalogues et leurs produits associes, avec authentification JWT, cache memoire, rate limiting et tracing OpenTelemetry.

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
| Tracing | OpenTelemetry (AspNetCore + Http instrumentation) |
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
  - `ExceptionHandlingMiddleware` : gestion globale des erreurs — message simple pour l'utilisateur, details complets (TraceId + stacktrace) dans les logs Serilog
  - `JwtMiddleware` : enrichit le `HttpContext` avec les claims utilisateur
- **Common** : `ApiResponse<T>`, `ApiError`

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

## OpenTelemetry

Les traces et metriques sont exportees vers la console en mode developpement.

```
Activity.DisplayName: POST /api/catalogues
Activity.Duration:    00:00:00.0234567
Activity.Tags:
    http.method: POST
    http.status_code: 201
```

Pour connecter un backend (Jaeger, Grafana Tempo, Azure Monitor), remplacer `AddConsoleExporter()` par `AddOtlpExporter()` dans `Program.cs`.

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

Reponse : `PagedResult<T>` avec `Items`, `TotalCount`, `Page`, `PageSize`.