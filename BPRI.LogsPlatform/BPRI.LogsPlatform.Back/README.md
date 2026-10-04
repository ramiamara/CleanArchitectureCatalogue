# BPRI.LogsPlatform.Back

API .NET 9 (lecture seule, accès libre) qui expose la table de logs écrite par `BPRI.ExceptionHandling`
(base `CatalogueLogsDb`, table `ExceptionLogs`) au front Angular `BPRI.LogsPlatform.Front`.

```bash
dotnet run --project BPRI.LogsPlatform/BPRI.LogsPlatform.Back      # http://localhost:5080
```

| Route | Description |
|---|---|
| `GET /api/projects` | Codes projet connus (`Cprj`), total et dernier log |
| `GET /api/logs` | Liste paginée : `project`, `application`, `from`, `to`, `severity`, `statusCode`, `exceptionType`, `traceId`, `search`, `page`, `pageSize`, `sortField`, `sortDir` |
| `GET /api/logs/{id}` | Détail (exception complète avec stack trace) |
| `GET /api/stats` | KPI, sévérités, codes HTTP, types, endpoints, chronologie (heure si ≤ 2 j, sinon jour) |
| `GET /api/filters` | Types d'exception, codes HTTP et applications présents (listes de filtres) |
| `GET /api/requests` | Requêtes tracées (table `RequestLogs`) : `project`, `application`, `method`, `statusCode`, `traceId`, `search`, `minDurationMs`, `from`, `to`, `page`, `pageSize`, `sortField`, `sortDir` |
| `GET /api/requests/{id}` | Détail d'une requête (corps de la requête et de la réponse) |
| `GET /api/requests/stats` | KPI (total, 4xx, 5xx, durée moyenne et P95), classes de statut, méthodes, endpoints les plus appelés et les plus lents, chronologie |
| `GET /api/requests/filters` | Applications, méthodes et codes HTTP présents |

La table `RequestLogs` n'existe que si `RequestTracing:Database:Enabled` est à `true` côté API tracée ; sinon les routes `/api/requests` renvoient des résultats vides.
| `GET /health` | Sonde |

- Configuration : `ConnectionStrings:ExceptionLogs`, `LogsTable:TableName` / `SchemaName` (doivent correspondre à `ExceptionHandling:Database` des applications), `Cors:AllowedOrigins`.
- Période par défaut : 24 h ; maximum 366 jours (sinon `400`).
- Pas d'authentification pour l'instant : à ajouter avant toute exposition hors poste de développement.
