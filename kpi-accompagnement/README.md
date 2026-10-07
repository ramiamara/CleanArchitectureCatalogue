# KPI Accompagnements — maquette Angular 21 + Chart.js

Maquette des pages **KPI-DA-1 / KPI-CONS-1**, **KPI-02 (Zoom vers inducteurs)** et **KPI-03 (matrice d'accompagnement)**, alimentée par des données mockées.

```bash
npm install
npm start          # http://localhost:4200  (/da ou /conseiller)
```

## Connexion et profils
L'écran `/connexion` propose des comptes de démo. Le profil détermine le périmètre :

| Profil | Voit | Détail par |
|---|---|---|
| Conseiller | ses propres accompagnements | — |
| DA | les conseillers de son agence | conseiller |
| DS | les agences de son secteur | agence |
| AN (animateur) | les conseillers qu'il a animés | conseiller |
| RA (responsable animateur) | les accompagnements de ses animateurs | animateur |

Toute la règle de périmètre est dans `core/services/perimetre.service.ts`, la config par profil dans `core/models/organisation.model.ts` (`PROFILS`).

## Structure
```
src/app/
  core/
    models/       organisation.model.ts · accompagnement.model.ts · kpi.model.ts
    mocks/        referentiels.mock.ts · organisation.mock.ts · accompagnements.mock.ts
    services/     auth.service.ts          utilisateur connecté (simulé)
                  kpi-data.service.ts      source de données → remplacer par HttpClient
                  perimetre.service.ts     ce que chaque profil peut voir
                  kpi-calcul.service.ts    RÈGLES DE CALCUL
                  kpi-store.service.ts     filtres + view-models (signals)
    guards/       auth.guard.ts
  layout/         header/ · filtres-panel/
  shared/
    components/   chart/ · kpi-tuile/ · score-ratio/ · page-header/
    pipes/        pts · date-fr · signe · atteinte
  features/
    connexion/
    dashboard/    page KPI-xx-1
      components/ pilotage-card · repartition-chart · resultats-card · synthese-resultats
                  bloc-coaching · bloc-atelier · atelier-activite-table · atelier-univers-table
                  detail-entites · frise-card
    inducteurs/   page KPI-xx-02
    coaching-detail/ page KPI-xx-03 (+ coaching-infos · coaching-evolution · coaching-matrice)
```

## Règles de calcul
| | Conseiller | DA / DS / AN / RA |
|---|---|---|
| Pilotage – coachings | en cours + terminés, début **ou** fin ∈ période | terminés, fin ∈ période |
| Pilotage – ateliers | en cours + terminés, date ∈ période | réalisés, date ∈ période |
| Résultats | terminés, fin ∈ période | idem |
| % indicateurs en progression | positifs / total | moyenne des % de chaque conseiller |
| +X pts | moyenne des seuls indicateurs positifs | idem |

- J/H : coaching = 1 journée réalisée = 1 J/H ; atelier = participants × jours (×0,5 si matin / après-midi).
- Progression coaching = % d'atteinte dernière journée réalisée − 1ère journée.
- Atelier : activité = valeur J vs référentiel ; univers de besoin = % J+2 mois − % jour J.
