import { Injectable, inject } from '@angular/core';
import { Atelier, Coaching, Typologie } from '../models/accompagnement.model';
import {
  BlocAtelierVm,
  BlocCoachingVm,
  Entite,
  FriseItemVm,
  InducteursVm,
  LigneDetailVm,
  Perimetre,
  PilotageVm,
  ResultatCollectifVm,
  ResultatIndividuelVm,
  SyntheseVm,
  TuileVm,
} from '../models/kpi.model';
import { Profil } from '../models/organisation.model';
import { INDICATEURS_BY_CODE, MOCK_TODAY, TYPOLOGIES_ATELIER, TYPOLOGIES_COACHING } from '../mocks/referentiels.mock';
import { addMonths, isBetween, moisEntre } from '../utils/date.utils';
import { KpiDataService } from './kpi-data.service';

/** Progression des indicateurs d'un accompagnement (1 coaching ou 1 participation à un atelier). */
interface ResultatUnitaire {
  collaborateurId: string;
  entite: Entite | null;
  pts: number[];
}

const moyenne = (v: number[]) => (v.length ? v.reduce((a, b) => a + b, 0) / v.length : 0);
const pourcentage = (realise: number, objectif: number) => (objectif ? (realise / objectif) * 100 : 0);
const arrondi1 = (n: number) => Math.round(n * 10) / 10;
const indicateurs = (t: Typologie) => [...t.activite, ...t.universBesoin];
const libelleCourt = (code: string) => INDICATEURS_BY_CODE[code]?.court ?? code;
const libelle = (code: string) => INDICATEURS_BY_CODE[code]?.libelle ?? code;

/**
 * Règles de calcul des KPI.
 *
 *  - Pilotage   : conseiller → en cours + terminés (date de début OU de fin dans la période)
 *                 autres     → coachings terminés (date de fin dans la période), ateliers réalisés
 *  - Résultats  : uniquement les accompagnements terminés dans la période
 *  - Progression coaching : % d'atteinte à la dernière journée − % à la première journée
 *  - Atelier    : activité = valeur J vs référentiel · univers de besoin = % J+2 mois − % J
 *  - % en progression : conseiller → positifs / total · autres → moyenne des % de chaque conseiller
 *  - +X pts     : moyenne des seuls indicateurs positifs (mail ALBANE du 17/09)
 */
@Injectable({ providedIn: 'root' })
export class KpiCalculService {
  private readonly data = inject(KpiDataService);

  // =========================================================================
  // Règles d'inclusion
  // =========================================================================
  inclusPilotage(c: Coaching, p: Perimetre, avecEnCours: boolean): boolean {
    const { dateDebut, dateFin } = p.filtres;
    if (!avecEnCours) return c.statut === 'TERMINE' && isBetween(c.dateFin, dateDebut, dateFin);
    return isBetween(c.dateDebut, dateDebut, dateFin) || isBetween(c.dateFin, dateDebut, dateFin);
  }

  inclusResultats(c: Coaching, p: Perimetre): boolean {
    return c.statut === 'TERMINE' && isBetween(c.dateFin, p.filtres.dateDebut, p.filtres.dateFin);
  }

  private atelierInclusPilotage(a: Atelier, p: Perimetre, avecEnCours: boolean): boolean {
    return isBetween(a.date, p.filtres.dateDebut, p.filtres.dateFin) && (avecEnCours || a.statut === 'TERMINE');
  }

  private atelierInclusResultats(a: Atelier, p: Perimetre): boolean {
    return a.statut === 'TERMINE' && isBetween(a.date, p.filtres.dateDebut, p.filtres.dateFin);
  }

  /** J/H d'un participant : nb jours × (1 si journée entière, ½ si matin / après-midi). */
  jhParParticipant(a: Atelier): number {
    return a.nbJours * (a.creneau === 'JOURNEE' ? 1 : 0.5);
  }

  // =========================================================================
  // 1. Pilotage
  // =========================================================================
  pilotage(p: Perimetre, avecEnCours: boolean): PilotageVm {
    const coachings = p.coachings.filter((c) => this.inclusPilotage(c, p, avecEnCours));
    const ateliers = p.ateliers.filter((a) => this.atelierInclusPilotage(a.atelier, p, avecEnCours));

    const jhCoaching = coachings
      .flatMap((c) => c.sessions)
      .filter((s) => s.realisee && isBetween(s.date, p.filtres.dateDebut, p.filtres.dateFin)).length;
    const jhAteliers = ateliers
      .filter((a) => a.atelier.date <= MOCK_TODAY)
      .reduce((total, a) => total + a.participants.length * this.jhParParticipant(a.atelier), 0);

    const nbParticipations = ateliers.reduce((total, a) => total + a.participants.length, 0);
    const total = coachings.length + nbParticipations;
    const collaborateurs = new Set([
      ...coachings.map((c) => c.collaborateurId),
      ...ateliers.flatMap((a) => a.participants.map((x) => x.collaborateurId)),
    ]);
    const pctIndividuels = total ? Math.round((coachings.length / total) * 100) : 0;

    return {
      nbAccompagnements: total,
      nbCoachings: coachings.length,
      nbAteliers: ateliers.length,
      nbParticipations,
      nbJourneesJH: jhCoaching + jhAteliers,
      nbCollaborateurs: collaborateurs.size,
      pctIndividuels,
      pctCollectifs: total ? 100 - pctIndividuels : 0,
    };
  }

  // =========================================================================
  // 2. Résultats — coachings individuels
  // =========================================================================
  /** Progression d'un indicateur entre la 1ère et la dernière journée réalisée. */
  progression(c: Coaching, code: string): TuileVm {
    const realisees = c.sessions.filter((s) => s.realisee);
    const debut = realisees[0]?.mesures[code];
    const fin = realisees[realisees.length - 1]?.mesures[code];
    const pctDebut = debut ? pourcentage(debut.realise, debut.objectif) : 0;
    const pctFin = fin ? pourcentage(fin.realise, fin.objectif) : 0;
    return { code, libelle: libelleCourt(code), pctDebut: arrondi1(pctDebut), pctFin: arrondi1(pctFin), pts: Math.round(pctFin - pctDebut) };
  }

  resultatIndividuel(p: Perimetre): ResultatIndividuelVm {
    const coachings = p.coachings.filter((c) => this.inclusResultats(c, p));

    const blocs: BlocCoachingVm[] = TYPOLOGIES_COACHING.map((typologie) => {
      const cs = coachings.filter((c) => c.typologie === typologie.code);
      const tuiles = (codes: string[]) => codes.map((code) => this.moyenneTuiles(code, cs.map((c) => this.progression(c, code))));
      const activite = tuiles(typologie.activite);
      const universBesoin = tuiles(typologie.universBesoin);
      return {
        typologie,
        coachings: cs,
        activite,
        universBesoin,
        nbPositifs: [...activite, ...universBesoin].filter((t) => t.pts > 0).length,
        nbTotal: activite.length + universBesoin.length,
      };
    }).filter((b) => b.coachings.length > 0);

    const unitaires: ResultatUnitaire[] = coachings.map((c) => ({
      collaborateurId: c.collaborateurId,
      entite: p.entiteDe(c.collaborateurId, c.animateurId),
      pts: indicateurs(TYPOLOGIES_COACHING.find((t) => t.code === c.typologie)!).map((code) => this.progression(c, code).pts),
    }));

    return { ...this.synthese(unitaires, p.profil), blocs };
  }

  private moyenneTuiles(code: string, tuiles: TuileVm[]): TuileVm {
    return {
      code,
      libelle: libelleCourt(code),
      pctDebut: arrondi1(moyenne(tuiles.map((t) => t.pctDebut))),
      pctFin: arrondi1(moyenne(tuiles.map((t) => t.pctFin))),
      pts: Math.round(moyenne(tuiles.map((t) => t.pts))),
    };
  }

  // =========================================================================
  // 3. Résultats — ateliers collectifs
  // =========================================================================
  resultatCollectif(p: Perimetre): ResultatCollectifVm {
    const ateliers = p.ateliers.filter((a) => this.atelierInclusResultats(a.atelier, p));

    const blocs: BlocAtelierVm[] = TYPOLOGIES_ATELIER.map((typologie) => {
      const scope = ateliers.filter((a) => a.atelier.typologie === typologie.code);
      const participants = scope.flatMap((a) => a.participants);

      const activite = typologie.activite.map((code) => {
        const valeurJ = moyenne(participants.map((x) => x.activite[code].valeur));
        const referentiel = moyenne(participants.map((x) => x.activite[code].referentiel));
        return { code, libelle: libelle(code), valeurJ: Math.round(valeurJ), referentiel: Math.round(referentiel), pts: Math.round(pourcentage(valeurJ, referentiel) - 100) };
      });
      const universBesoin = typologie.universBesoin.map((code) => {
        const valeurJ = moyenne(participants.map((x) => x.universJ[code]));
        const valeurJ2 = moyenne(participants.map((x) => x.universJ2[code]));
        return { code, libelle: libelle(code), valeurJ: Math.round(valeurJ), valeurJ2: Math.round(valeurJ2), pts: Math.round(valeurJ2 - valeurJ) };
      });

      const dateJ = scope.map((a) => a.atelier.date).sort().at(-1) ?? '';
      return {
        typologie,
        dateJ,
        dateJ2: dateJ ? addMonths(dateJ, 2) : '',
        nbAteliers: scope.length,
        nbParticipants: participants.length,
        activite,
        universBesoin,
        nbPositifs: [...activite, ...universBesoin].filter((l) => l.pts > 0).length,
        nbTotal: activite.length + universBesoin.length,
      };
    }).filter((b) => b.nbAteliers > 0);

    const unitaires: ResultatUnitaire[] = ateliers.flatMap(({ atelier, participants }) => {
      const typologie = TYPOLOGIES_ATELIER.find((t) => t.code === atelier.typologie)!;
      return participants.map((x) => ({
        collaborateurId: x.collaborateurId,
        entite: p.entiteDe(x.collaborateurId, atelier.animateurId),
        pts: [
          ...typologie.activite.map((code) => pourcentage(x.activite[code].valeur, x.activite[code].referentiel) - 100),
          ...typologie.universBesoin.map((code) => x.universJ2[code] - x.universJ[code]),
        ],
      }));
    });

    return { ...this.synthese(unitaires, p.profil), blocs };
  }

  // =========================================================================
  // Synthèse commune : % en progression, +X pts, détail par entité
  // =========================================================================
  private synthese(unitaires: ResultatUnitaire[], profil: Profil): SyntheseVm {
    const tous = unitaires.flatMap((u) => u.pts);
    const positifs = tous.filter((v) => v > 0);

    const pctProgression =
      profil === 'CONSEILLER' ? (tous.length ? (positifs.length / tous.length) * 100 : 0) : this.moyenneParConseiller(unitaires);

    const parEntite = new Map<string, ResultatUnitaire[]>();
    for (const u of unitaires) {
      if (u.entite) parEntite.set(u.entite.id, [...(parEntite.get(u.entite.id) ?? []), u]);
    }
    const detail: LigneDetailVm[] = [...parEntite.values()]
      .map((items) => {
        const pts = items.flatMap((i) => i.pts);
        const pos = pts.filter((v) => v > 0);
        return {
          entite: items[0].entite!,
          nbAccompagnements: items.length,
          nbIndicateurs: pts.length,
          nbPositifs: pos.length,
          pctProgression: Math.round(this.moyenneParConseiller(items)),
          ptsMoyen: Math.round(moyenne(pos)),
        };
      })
      .sort((a, b) => a.entite.libelle.localeCompare(b.entite.libelle));

    return { pctProgression: Math.round(pctProgression), ptsMoyen: Math.round(moyenne(positifs)), detail };
  }

  /** Moyenne des % d'indicateurs en progression de chaque conseiller. */
  private moyenneParConseiller(unitaires: ResultatUnitaire[]): number {
    const parConseiller = new Map<string, number[]>();
    for (const u of unitaires) parConseiller.set(u.collaborateurId, [...(parConseiller.get(u.collaborateurId) ?? []), ...u.pts]);
    return moyenne([...parConseiller.values()].map((pts) => (pts.filter((v) => v > 0).length / pts.length) * 100));
  }

  // =========================================================================
  // 4. Frise des coachings vs période
  // =========================================================================
  frise(p: Perimetre, avecEnCours: boolean): FriseItemVm[] {
    return p.coachings
      .map((c) => ({
        coaching: c,
        libelle: `${this.data.nomCourt(c.collaborateurId)} · ${TYPOLOGIES_COACHING.find((t) => t.code === c.typologie)?.libelle}`,
        inclusResultats: this.inclusResultats(c, p),
        inclusPilotage: this.inclusPilotage(c, p, avecEnCours),
      }))
      .sort((a, b) => a.coaching.dateDebut.localeCompare(b.coaching.dateDebut));
  }

  // =========================================================================
  // 5. Inducteurs (zoom du pilotage)
  // =========================================================================
  inducteurs(p: Perimetre, avecEnCours: boolean): InducteursVm {
    const { dateDebut, dateFin } = p.filtres;
    const coachings = p.coachings.filter((c) => this.inclusPilotage(c, p, avecEnCours));
    const ateliers = p.ateliers.filter((a) => this.atelierInclusPilotage(a.atelier, p, avecEnCours));
    const ateliersRealises = ateliers.filter((a) => a.atelier.date <= MOCK_TODAY);
    const journees = coachings.flatMap((c) =>
      c.sessions.filter((s) => s.realisee && isBetween(s.date, dateDebut, dateFin)).map((s) => ({ date: s.date, coaching: c })),
    );

    // J/H par entité (ou par conseiller si le profil n'a pas d'axe de détail)
    const parEntite = new Map<string, { libelle: string; coaching: number; atelier: number }>();
    const cumuler = (collaborateurId: string, animateurId: string, coaching: number, atelier: number) => {
      const e = p.entiteDe(collaborateurId, animateurId) ?? { id: collaborateurId, libelle: this.data.nomCourt(collaborateurId) };
      const ligne = parEntite.get(e.id) ?? { libelle: e.libelle, coaching: 0, atelier: 0 };
      ligne.coaching += coaching;
      ligne.atelier += atelier;
      parEntite.set(e.id, ligne);
    };
    journees.forEach((j) => cumuler(j.coaching.collaborateurId, j.coaching.animateurId, 1, 0));
    ateliersRealises.forEach((a) => a.participants.forEach((x) => cumuler(x.collaborateurId, a.atelier.animateurId, 0, this.jhParParticipant(a.atelier))));

    return {
      jhParMois: moisEntre(dateDebut, dateFin).map((mois) => ({
        mois,
        coaching: journees.filter((j) => j.date.startsWith(mois)).length,
        atelier: ateliersRealises
          .filter((a) => a.atelier.date.startsWith(mois))
          .reduce((t, a) => t + a.participants.length * this.jhParParticipant(a.atelier), 0),
      })),
      parTypologie: [
        ...TYPOLOGIES_COACHING.map((t) => ({ libelle: t.libelle, estAtelier: false, nb: coachings.filter((c) => c.typologie === t.code).length })),
        ...TYPOLOGIES_ATELIER.map((t) => ({
          libelle: t.libelle,
          estAtelier: true,
          nb: ateliers.filter((a) => a.atelier.typologie === t.code).reduce((n, a) => n + a.participants.length, 0),
        })),
      ].filter((x) => x.nb > 0),
      jhParEntite: [...parEntite.values()].sort((a, b) => a.libelle.localeCompare(b.libelle)),
      court: coachings.filter((c) => c.duree === 'COURT').length + ateliers.filter((a) => a.atelier.duree === 'COURT').length,
      long: coachings.filter((c) => c.duree === 'LONG').length + ateliers.filter((a) => a.atelier.duree === 'LONG').length,
    };
  }
}
