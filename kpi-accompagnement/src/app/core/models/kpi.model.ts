import { Atelier, Coaching, DureeAccompagnement, ParticipationAtelier, Typologie } from './accompagnement.model';
import { Metier, Profil } from './organisation.model';

// ---------------------------------------------------------------------------
// Filtres
// ---------------------------------------------------------------------------
export type TypeAccompagnement = 'TOUS' | 'COACHING' | 'ATELIER';

export interface KpiFiltres {
  dateDebut: string;
  dateFin: string;
  metier: Metier | 'TOUS';
  /** Agence (DS), conseiller (DA / AN) ou animateur (RA). */
  entiteId: string;
  type: TypeAccompagnement;
  typologie: string;
  duree: DureeAccompagnement | 'TOUS';
}

// ---------------------------------------------------------------------------
// Périmètre de calcul (ce que l'utilisateur a le droit de voir, filtres appliqués)
// ---------------------------------------------------------------------------
export interface AtelierScope {
  atelier: Atelier;
  participants: ParticipationAtelier[];
}

/** Ligne du tableau de détail (agence, conseiller ou animateur). */
export interface Entite {
  id: string;
  libelle: string;
  sousLibelle?: string;
}

export interface Perimetre {
  profil: Profil;
  filtres: KpiFiltres;
  coachings: Coaching[];
  ateliers: AtelierScope[];
  /** Entité de ventilation d'un accompagnement (null si pas de détail pour ce profil). */
  entiteDe: (collaborateurId: string, animateurId: string) => Entite | null;
}

// ---------------------------------------------------------------------------
// View models
// ---------------------------------------------------------------------------
export interface PilotageVm {
  nbAccompagnements: number;
  nbCoachings: number;
  nbAteliers: number;
  nbParticipations: number;
  nbJourneesJH: number;
  nbCollaborateurs: number;
  pctIndividuels: number;
  pctCollectifs: number;
}

export interface TuileVm {
  code: string;
  libelle: string;
  pts: number;
  pctDebut: number;
  pctFin: number;
}

export interface BlocCoachingVm {
  typologie: Typologie;
  coachings: Coaching[];
  activite: TuileVm[];
  universBesoin: TuileVm[];
  nbPositifs: number;
  nbTotal: number;
}

export interface LigneActiviteAtelierVm {
  code: string;
  libelle: string;
  valeurJ: number;
  referentiel: number;
  pts: number;
}

export interface LigneUniversAtelierVm {
  code: string;
  libelle: string;
  valeurJ: number;
  valeurJ2: number;
  pts: number;
}

export interface BlocAtelierVm {
  typologie: Typologie;
  dateJ: string;
  dateJ2: string;
  nbAteliers: number;
  nbParticipants: number;
  activite: LigneActiviteAtelierVm[];
  universBesoin: LigneUniversAtelierVm[];
  nbPositifs: number;
  nbTotal: number;
}

export interface LigneDetailVm {
  entite: Entite;
  nbAccompagnements: number;
  nbIndicateurs: number;
  nbPositifs: number;
  pctProgression: number;
  ptsMoyen: number;
}

export interface SyntheseVm {
  pctProgression: number;
  ptsMoyen: number;
  detail: LigneDetailVm[];
}

export interface ResultatIndividuelVm extends SyntheseVm {
  blocs: BlocCoachingVm[];
}

export interface ResultatCollectifVm extends SyntheseVm {
  blocs: BlocAtelierVm[];
}

export interface FriseItemVm {
  coaching: Coaching;
  libelle: string;
  inclusResultats: boolean;
  inclusPilotage: boolean;
}

export interface InducteursVm {
  jhParMois: { mois: string; coaching: number; atelier: number }[];
  parTypologie: { libelle: string; nb: number; estAtelier: boolean }[];
  jhParEntite: { libelle: string; coaching: number; atelier: number }[];
  court: number;
  long: number;
}
