import { Atelier, Coaching, CreneauAtelier, Mesure, ParticipationAtelier, SessionCoaching } from '../models/accompagnement.model';
import { addDays } from '../utils/date.utils';
import { ANIMATEUR_PAR_AGENCE, COLLABORATEURS } from './organisation.mock';
import { INDICATEURS_BY_CODE, MOCK_TODAY, TYPOLOGIES_ATELIER, TYPOLOGIES_COACHING } from './referentiels.mock';

// Générateur pseudo-aléatoire à graine fixe : mêmes données à chaque lancement.
let seed = 20261007;
function rnd(): number {
  seed = (seed + 0x6d2b79f5) | 0;
  let t = Math.imul(seed ^ (seed >>> 15), 1 | seed);
  t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
  return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
}
const between = (min: number, max: number) => min + rnd() * (max - min);
const int = (min: number, max: number) => Math.round(between(min, max));
const pick = <T>(arr: T[]): T => arr[Math.floor(rnd() * arr.length)];
const round2 = (n: number) => Math.round(n * 100) / 100;

const agenceDe = (collaborateurId: string) => COLLABORATEURS.find((c) => c.id === collaborateurId)!.agenceId;

/** 80 % du temps l'animateur de l'agence, sinon un autre. */
const animateurPour = (collaborateurId: string) =>
  rnd() < 0.8 ? ANIMATEUR_PAR_AGENCE[agenceDe(collaborateurId)] : pick(['AN1', 'AN2', 'AN3']);

function objectifBase(code: string): number {
  const unite = INDICATEURS_BY_CODE[code]?.unite;
  if (unite === '%') return int(50, 95);
  if (unite === '€') return int(40, 120) * 1000;
  if (code === 'RDV') return int(25, 60);
  return int(5, 40);
}

// ---------------------------------------------------------------------------
// Coachings
// ---------------------------------------------------------------------------
function genererSessions(indicateurs: string[], dateDebut: string, nbSessions: number): SessionCoaching[] {
  const ecart = int(25, 45);
  const evolutions = indicateurs.map((code) => {
    const debut = between(55, 140);
    const delta = rnd() < 0.6 ? between(5, 110) : -between(5, 40); // ~60 % en progression
    return { code, objectif: objectifBase(code), debut, fin: Math.max(10, debut + delta) };
  });

  return Array.from({ length: nbSessions }, (_, i) => {
    const date = addDays(dateDebut, i * ecart);
    const t = i / (nbSessions - 1);
    const mesures: Record<string, Mesure> = {};
    for (const e of evolutions) {
      const bruit = i === 0 || i === nbSessions - 1 ? 0 : between(-8, 8);
      const pct = e.debut + (e.fin - e.debut) * t + bruit;
      const estTaux = INDICATEURS_BY_CODE[e.code]?.unite === '%';
      const objectif = estTaux ? e.objectif : Math.round(e.objectif * (0.4 + 0.6 * t)); // objectif proratisé
      const realise = estTaux ? Math.min(100, round2((objectif * pct) / 100)) : Math.round((objectif * pct) / 100);
      mesures[e.code] = { objectif, realise };
    }
    return { date, realisee: date <= MOCK_TODAY, mesures };
  });
}

let numCoaching = 1;
function coaching(collaborateurId: string, typologie: string, dateDebut: string, nbSessions: number): Coaching {
  const typo = TYPOLOGIES_COACHING.find((t) => t.code === typologie)!;
  const sessions = genererSessions([...typo.activite, ...typo.universBesoin], dateDebut, nbSessions);
  const dateFin = sessions[sessions.length - 1].date;
  return {
    id: `COA-${String(numCoaching++).padStart(3, '0')}`,
    collaborateurId,
    animateurId: animateurPour(collaborateurId),
    typologie,
    statut: dateFin <= MOCK_TODAY ? 'TERMINE' : 'EN_COURS',
    duree: nbSessions >= 5 ? 'LONG' : 'COURT',
    dateDebut,
    dateFin,
    sessions,
  };
}

function genererCoachings(): Coaching[] {
  // Conseiller de démo (C1) : 3 coachings
  const res = [
    coaching('C1', 'EFF_COM', '2025-12-18', 6),
    coaching('C1', 'VCC', '2026-02-10', 4),
    coaching('C1', 'EPARGNE', '2026-07-06', 5),
  ];
  for (const collab of COLLABORATEURS.filter((c) => c.id !== 'C1')) {
    const nb = int(1, 3);
    for (let i = 0; i < nb; i++) {
      res.push(coaching(collab.id, pick(TYPOLOGIES_COACHING).code, addDays('2025-10-01', int(0, 330)), int(3, 6)));
    }
  }
  return res;
}

// ---------------------------------------------------------------------------
// Ateliers collectifs
// ---------------------------------------------------------------------------
function participation(collaborateurId: string, typologie: string): ParticipationAtelier {
  const typo = TYPOLOGIES_ATELIER.find((t) => t.code === typologie)!;
  const referentiels: Record<string, number> = { APPELS_PASSES: 40, APPELS_ABOUTIS: 20, DUREE_PHONING: 120, RDV_OBTENUS: 6 };
  const activite: ParticipationAtelier['activite'] = {};
  for (const code of typo.activite) {
    const referentiel = referentiels[code] ?? 10;
    activite[code] = { referentiel, valeur: Math.round(referentiel * between(0.6, 1.5)) };
  }
  const universJ: Record<string, number> = {};
  const universJ2: Record<string, number> = {};
  for (const code of typo.universBesoin) {
    universJ[code] = Math.round(between(0, 260));
    universJ2[code] = Math.max(0, Math.round(universJ[code] + (rnd() < 0.65 ? between(5, 140) : -between(5, 60))));
  }
  return { collaborateurId, activite, universJ, universJ2 };
}

function genererAteliers(): Atelier[] {
  const plan: [string, string, string, number, CreneauAtelier, string[]][] = [
    // typologie, date, animateur, nbJours, créneau, participants
    ['AT_PHONING', '2026-01-15', 'AN1', 1, 'JOURNEE', ['C2', 'C3', 'C5']],
    ['AT_PHONING', '2026-03-01', 'AN1', 1, 'MATIN', ['C1', 'C4', 'C6']],
    ['AT_ASV', '2026-04-09', 'AN1', 2, 'JOURNEE', ['C1', 'C2', 'C3', 'C5']],
    ['AT_PREVOYANCE', '2026-05-20', 'AN2', 1, 'APRES_MIDI', ['C7', 'C8', 'C10', 'C12']],
    ['AT_PHONING', '2026-06-11', 'AN2', 1, 'JOURNEE', ['C9', 'C11', 'C13', 'C14']],
    ['AT_ASV', '2026-09-17', 'AN2', 1, 'MATIN', ['C7', 'C12', 'C15']],
    ['AT_PHONING', '2026-02-24', 'AN3', 1, 'JOURNEE', ['C16', 'C17', 'C18']],
    ['AT_ASV', '2026-06-30', 'AN3', 1, 'JOURNEE', ['C16', 'C18', 'C4']],
    ['AT_PREVOYANCE', '2026-11-05', 'AN1', 1, 'JOURNEE', ['C1', 'C6']], // à venir
  ];
  return plan.map(([typologie, date, animateurId, nbJours, creneau, participants], i) => ({
    id: `ATE-${String(i + 1).padStart(3, '0')}`,
    typologie,
    animateurId,
    statut: date <= MOCK_TODAY ? 'TERMINE' : 'EN_COURS',
    duree: nbJours > 1 ? 'LONG' : 'COURT',
    date,
    nbJours,
    creneau,
    participants: participants.map((id) => participation(id, typologie)),
  }));
}

export const COACHINGS_MOCK: Coaching[] = genererCoachings();
export const ATELIERS_MOCK: Atelier[] = genererAteliers();
