# BPRI.LogsPlatform.Front (Angular 22 + Angular Material)

Interface de consultation des logs d'exceptions (type Grafana) : sélecteur de projet (`cprj`, ex. `CatalogueAPP`),
recherche par TraceId, période, KPI, graphiques (sévérité Error/Warning/Info, codes HTTP dont 403, types d'exception,
chronologie, endpoints), tableau filtrable/paginé et détail d'un log. Accès libre, thèmes clair et sombre
(bouton en haut à droite, mémorisé, suit la préférence du système au premier lancement).

Interface : Angular Material 22 (thème Material 3). Graphiques : Chart.js (Material n'a pas de composant de graphiques).

## Démarrage

```bash
npm install
npm start            # http://localhost:4200  (proxy /api -> http://localhost:5080, voir proxy.conf.json)
```

L'API est `BPRI.LogsPlatform.Back` (`dotnet run`, port 5080).

### Sans SQL Server : faux serveur d'API

```bash
npm run build && node mock/server.mjs     # http://localhost:5080 (sert aussi le build)
```

## Prérequis
Node.js >= 22.22.3 (ou >= 24.15) pour Angular CLI 22.
