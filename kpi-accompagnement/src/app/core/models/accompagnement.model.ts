export type StatutAccompagnement = 'EN_COURS' | 'TERMINE';
export type DureeAccompagnement = 'COURT' | 'LONG';
export type CreneauAtelier = 'JOURNEE' | 'MATIN' | 'APRES_MIDI';

/** Indicateur suivi (fichier "Indicateurs à suivre.xlsx", onglet "Indicateurs - Niv 1"). */
export interface Indicateur {
  code: string;
  libelle: string;
  court: string;
  unite: '%' | 'nb' | '€' | 'min';
}

/** Typologie (libellé) d'un coaching individuel ou d'un atelier collectif. */
export interface Typologie {
  code: string;
  libelle: string;
  activite: string[]; // codes indicateurs
  universBesoin: string[]; // codes indicateurs
}

/** Valeur réalisée vs objectif. */
export interface Mesure {
  realise: number;
  objectif: number;
}

/** Journée de coaching : 1 journée réalisée = 1 J/H. */
export interface SessionCoaching {
  date: string; // ISO yyyy-mm-dd
  realisee: boolean;
  mesures: Record<string, Mesure>;
}

export interface Coaching {
  id: string;
  collaborateurId: string;
  animateurId: string;
  typologie: string;
  statut: StatutAccompagnement;
  duree: DureeAccompagnement;
  dateDebut: string;
  dateFin: string;
  sessions: SessionCoaching[];
}

export interface ParticipationAtelier {
  collaborateurId: string;
  /** Activité au jour J : valeur et référentiel. */
  activite: Record<string, { valeur: number; referentiel: number }>;
  /** Univers de besoin : % d'atteinte au jour J et à J+2 mois. */
  universJ: Record<string, number>;
  universJ2: Record<string, number>;
}

export interface Atelier {
  id: string;
  typologie: string;
  animateurId: string;
  statut: StatutAccompagnement;
  duree: DureeAccompagnement;
  date: string;
  nbJours: number;
  creneau: CreneauAtelier;
  participants: ParticipationAtelier[];
}
