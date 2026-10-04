# BPRI.LogsPlatform.Front (Angular 21 + Angular Material)

Interface de consultation des logs (type Grafana), en accès libre, thèmes clair et sombre (bouton en haut à droite, mémorisé).

Barre du haut : projet (`cprj`, par défaut `CatalogueAPP`), **application**, période, recherche par **TraceId** (toute période).

- **Exceptions** : KPI, graphiques (sévérité, chronologie, codes HTTP, types d'exception, endpoints), tableau filtrable/paginé, détail d'un log.
- **Requêtes** : KPI (total, 4xx, 5xx, durée moyenne et P95), graphiques (statuts, chronologie avec durée moyenne, codes HTTP, méthodes), endpoints les plus appelés et les plus lents, tableau filtrable (méthode, code, durée minimale, texte) avec détail (corps de la requête et de la réponse).
  Les données viennent de la table `RequestLogs`, remplie si `RequestTracing:Database:Enabled` vaut `true` côté API tracée.

Depuis un détail, on passe d'une requête à ses exceptions (et inversement) grâce au TraceId.

## Démarrage

```bash
npm install
npm start            # http://localhost:4200  (proxy /api -> http://localhost:5080, voir proxy.conf.json)
```

L'API est `BPRI.LogsPlatform.Back` (`dotnet run`, port 5080).

### Sans SQL Server : faux serveur d'API

```bash
npm run build && npm run mock     # http://localhost:5080 (sert aussi le build)
```

## Prérequis
Node.js 20.19+, 22.12+ ou 24+ (Angular 21). Application sans zone.js (zoneless), thème Material 3.
