# BPRI.LogsPlatform.Back

API .NET 9 (lecture seule, accès libre) qui expose la table de logs écrite par `BPRI.ExceptionHandling`
(base `CatalogueLogsDb`, table `ExceptionLogs`) au front Angular `BPRI.LogsPlatform.Front`.

```bash
dotnet run --project BPRI.LogsPlatform/BPRI.LogsPlatform.Back      # http://localhost:5080
```

| Route | Description |
|---|---|
| `GET /api/projects` | Codes projet connus (`Cprj`), total et dernier log |
| `GET /api/logs` | Liste paginée : `project`, `from`, `to`, `severity`, `statusCode`, `exceptionType`, `traceId`, `search`, `page`, `pageSize`, `sortField`, `sortDir` |
| `GET /api/logs/{id}` | Détail (exception complète avec stack trace) |
| `GET /api/stats` | KPI, sévérités, codes HTTP, types, endpoints, chronologie (heure si ≤ 2 j, sinon jour) |
| `GET /api/filters` | Types d'exception et codes HTTP présents (listes de filtres) |
| `GET /health` | Sonde |

- Configuration : `ConnectionStrings:ExceptionLogs`, `LogsTable:TableName` / `SchemaName` (doivent correspondre à `ExceptionHandling:Database` des applications), `Cors:AllowedOrigins`.
- Période par défaut : 24 h ; maximum 366 jours (sinon `400`).
- Pas d'authentification pour l'instant : à ajouter avant toute exposition hors poste de développement.
