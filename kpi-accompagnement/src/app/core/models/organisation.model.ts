/** Les 5 profils de connexion. */
export type Profil = 'CONSEILLER' | 'DA' | 'DS' | 'AN' | 'RA';

export type Metier = 'PART' | 'PRO';

export interface Secteur {
  id: string;
  libelle: string;
  directeur: string; // DS
}

export interface Agence {
  id: string;
  libelle: string;
  secteurId: string;
  directeur: string; // DA
}

export interface Collaborateur {
  id: string;
  prenom: string;
  nom: string;
  agenceId: string;
  metier: Metier;
}

export interface Animateur {
  id: string;
  prenom: string;
  nom: string;
  responsableId: string; // Responsable animateur
}

export interface ResponsableAnimateur {
  id: string;
  prenom: string;
  nom: string;
}

/**
 * Utilisateur connecté. `perimetreId` dépend du profil :
 * CONSEILLER → collaborateurId · DA → agenceId · DS → secteurId · AN → animateurId · RA → responsableId
 */
export interface Utilisateur {
  id: string;
  nomComplet: string;
  profil: Profil;
  perimetreId: string;
}

/** Axe de ventilation du tableau de détail ("par agence", "par conseiller", ...). */
export type AxeDetail = 'COLLABORATEUR' | 'AGENCE' | 'ANIMATEUR';

export interface ProfilConfig {
  libelle: string;
  /** Code utilisé dans les noms de pages (KPI-DA-1, KPI-CONS-1...). */
  code: string;
  /** Ventilation affichée sous les résultats (null = pas de tableau de détail). */
  axeDetail: AxeDetail | null;
  /** Pilotage : prend-on aussi les coachings / ateliers en cours ? */
  pilotageAvecEnCours: boolean;
}

export const PROFILS: Record<Profil, ProfilConfig> = {
  CONSEILLER: { libelle: 'Conseiller', code: 'CONS', axeDetail: null, pilotageAvecEnCours: true },
  DA: { libelle: "Directeur d'agence", code: 'DA', axeDetail: 'COLLABORATEUR', pilotageAvecEnCours: false },
  DS: { libelle: 'Directeur de secteur', code: 'DS', axeDetail: 'AGENCE', pilotageAvecEnCours: false },
  AN: { libelle: 'Animateur', code: 'AN', axeDetail: 'COLLABORATEUR', pilotageAvecEnCours: false },
  RA: { libelle: 'Responsable animateur', code: 'RA', axeDetail: 'ANIMATEUR', pilotageAvecEnCours: false },
};

export const LIBELLE_AXE: Record<AxeDetail, string> = {
  COLLABORATEUR: 'Conseiller',
  AGENCE: 'Agence',
  ANIMATEUR: 'Animateur',
};
