import { Agence, Animateur, Collaborateur, ResponsableAnimateur, Secteur, Utilisateur } from '../models/organisation.model';

export const SECTEURS: Secteur[] = [
  { id: 'S1', libelle: "Val-d'Oise Ouest", directeur: 'Marc Lefèvre' },
  { id: 'S2', libelle: 'Yvelines Nord', directeur: 'Sophie Marchand' },
];

export const AGENCES: Agence[] = [
  { id: 'AG1', libelle: 'CONFLANS - HERBLAY', secteurId: 'S1', directeur: 'Claire Dumont' },
  { id: 'AG2', libelle: 'CERGY - PONTOISE', secteurId: 'S1', directeur: 'Paul Garnier' },
  { id: 'AG3', libelle: 'SAINT-OUEN-L’AUMÔNE', secteurId: 'S1', directeur: 'Nadia Benhamou' },
  { id: 'AG4', libelle: 'SARTROUVILLE', secteurId: 'S2', directeur: 'Olivier Caron' },
];

export const RESPONSABLES_ANIMATEURS: ResponsableAnimateur[] = [{ id: 'RA1', prenom: 'Isabelle', nom: 'Roche' }];

export const ANIMATEURS: Animateur[] = [
  { id: 'AN1', prenom: 'Élodie', nom: 'Perret', responsableId: 'RA1' },
  { id: 'AN2', prenom: 'Karim', nom: 'Saïdi', responsableId: 'RA1' },
  { id: 'AN3', prenom: 'Thomas', nom: 'Blanc', responsableId: 'RA1' },
];

const c = (id: string, prenom: string, nom: string, agenceId: string, metier: 'PART' | 'PRO' = 'PART'): Collaborateur => ({
  id,
  prenom,
  nom,
  agenceId,
  metier,
});

export const COLLABORATEURS: Collaborateur[] = [
  c('C1', 'Moez', 'Ben Ali', 'AG1'),
  c('C2', 'Hugo', 'Lambert', 'AG1'),
  c('C3', 'Sarah', 'Martin', 'AG1'),
  c('C4', 'Guillaume', 'Petit', 'AG1', 'PRO'),
  c('C5', 'Inès', 'Kader', 'AG1'),
  c('C6', 'Julien', 'Moreau', 'AG1', 'PRO'),
  c('C7', 'Camille', 'Roux', 'AG2'),
  c('C8', 'Yanis', 'Haddad', 'AG2'),
  c('C9', 'Laura', 'Fontaine', 'AG2', 'PRO'),
  c('C10', 'Mehdi', 'Amrani', 'AG2'),
  c('C11', 'Chloé', 'Girard', 'AG2'),
  c('C12', 'Antoine', 'Faure', 'AG3'),
  c('C13', 'Léa', 'Mercier', 'AG3', 'PRO'),
  c('C14', 'Sofiane', 'Belkacem', 'AG3'),
  c('C15', 'Manon', 'Lopez', 'AG3'),
  c('C16', 'Nicolas', 'Perrin', 'AG4'),
  c('C17', 'Emma', 'Gauthier', 'AG4'),
  c('C18', 'Rayan', 'Diallo', 'AG4', 'PRO'),
];

/** Animateur "attitré" de chaque agence (utilisé par le générateur de données). */
export const ANIMATEUR_PAR_AGENCE: Record<string, string> = { AG1: 'AN1', AG2: 'AN2', AG3: 'AN2', AG4: 'AN3' };

/** Comptes de démonstration proposés sur l'écran de connexion. */
export const UTILISATEURS: Utilisateur[] = [
  { id: 'U1', nomComplet: 'Moez Ben Ali', profil: 'CONSEILLER', perimetreId: 'C1' },
  { id: 'U2', nomComplet: 'Claire Dumont', profil: 'DA', perimetreId: 'AG1' },
  { id: 'U3', nomComplet: 'Paul Garnier', profil: 'DA', perimetreId: 'AG2' },
  { id: 'U4', nomComplet: 'Marc Lefèvre', profil: 'DS', perimetreId: 'S1' },
  { id: 'U5', nomComplet: 'Élodie Perret', profil: 'AN', perimetreId: 'AN1' },
  { id: 'U6', nomComplet: 'Karim Saïdi', profil: 'AN', perimetreId: 'AN2' },
  { id: 'U7', nomComplet: 'Isabelle Roche', profil: 'RA', perimetreId: 'RA1' },
];
