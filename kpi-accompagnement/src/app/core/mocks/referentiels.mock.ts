import { Indicateur, Typologie } from '../models/accompagnement.model';

/** Date "du jour" figée pour que la maquette soit reproductible. */
export const MOCK_TODAY = '2026-10-07';

export const INDICATEURS: Indicateur[] = [
  { code: 'RDV', libelle: 'Nombre de RDV', court: 'RDV', unite: 'nb' },
  { code: 'TX_SUCCES', libelle: 'Taux de succès', court: 'Succès', unite: '%' },
  { code: 'TX_MLV', libelle: 'Taux de multiventes', court: 'MLV', unite: '%' },
  { code: 'TX_PREP', libelle: "Taux de préparation d'entretien", court: 'Prépa. entretien', unite: '%' },
  { code: 'NER_PART', libelle: 'NER PART nb', court: 'NER', unite: 'nb' },
  { code: 'IARD_PART', libelle: 'IARD PART nb', court: 'IARD', unite: 'nb' },
  { code: 'PVC_PART', libelle: 'PVC PART nb', court: 'PVC', unite: 'nb' },
  { code: 'BANCASS_PART', libelle: 'BANCASSURANCE PART nb', court: 'Bancassurance', unite: 'nb' },
  { code: 'VCC_NB', libelle: 'VCC nombre', court: 'VCC nombre', unite: 'nb' },
  { code: 'VCC_MT', libelle: 'VCC montant', court: 'VCC montant', unite: '€' },
  { code: 'ASV_NB', libelle: 'ASV (Nb)', court: 'ASV (Nb)', unite: 'nb' },
  { code: 'ASV_VP', libelle: 'ASV (VP)', court: 'ASV (VP)', unite: 'nb' },
  { code: 'ASV_MTT', libelle: 'ASV (MTT)', court: 'ASV (MTT)', unite: '€' },
  { code: 'ASSUR_VIE', libelle: 'Assur. Vie', court: 'Assur. Vie', unite: 'nb' },
  { code: 'APPELS_PASSES', libelle: 'Appels passés', court: 'Appels passés', unite: 'nb' },
  { code: 'APPELS_ABOUTIS', libelle: 'Appels aboutis', court: 'Appels aboutis', unite: 'nb' },
  { code: 'DUREE_PHONING', libelle: 'Durée phoning', court: 'Durée phoning', unite: 'min' },
  { code: 'RDV_OBTENUS', libelle: 'Rdv obtenus', court: 'Rdv obtenus', unite: 'nb' },
];

export const INDICATEURS_BY_CODE: Record<string, Indicateur> = Object.fromEntries(
  INDICATEURS.map((i) => [i.code, i]),
);

/** Référentiel des 7 coachings individuels. */
export const TYPOLOGIES_COACHING: Typologie[] = [
  {
    code: 'EFF_COM',
    libelle: 'Efficacité commerciale',
    activite: ['RDV', 'TX_SUCCES', 'TX_MLV', 'TX_PREP'],
    universBesoin: ['NER_PART', 'IARD_PART', 'PVC_PART', 'BANCASS_PART', 'ASV_NB'],
  },
  { code: 'VCC', libelle: 'VCC', activite: ['VCC_MT', 'VCC_NB'], universBesoin: ['ASV_NB', 'ASSUR_VIE'] },
  {
    code: 'PROSPECTION',
    libelle: 'Prospection & prise de RDV',
    activite: ['RDV', 'TX_PREP'],
    universBesoin: ['NER_PART', 'ASV_NB', 'VCC_NB'],
  },
  {
    code: 'EPARGNE',
    libelle: 'Épargne financière',
    activite: ['TX_SUCCES', 'TX_MLV'],
    universBesoin: ['ASV_NB', 'ASV_VP', 'ASSUR_VIE'],
  },
  { code: 'PREVOYANCE', libelle: 'Prévoyance & IARD', activite: ['RDV', 'TX_SUCCES'], universBesoin: ['IARD_PART', 'PVC_PART'] },
  {
    code: 'CONDUITE',
    libelle: "Conduite d'entretien",
    activite: ['TX_PREP', 'TX_SUCCES', 'TX_MLV'],
    universBesoin: ['BANCASS_PART', 'NER_PART'],
  },
  { code: 'ORGA', libelle: 'Organisation commerciale', activite: ['RDV', 'TX_PREP'], universBesoin: ['NER_PART', 'IARD_PART'] },
];

/** Référentiel des ateliers collectifs. */
export const TYPOLOGIES_ATELIER: Typologie[] = [
  {
    code: 'AT_PHONING',
    libelle: 'Atelier Phoning',
    activite: ['APPELS_PASSES', 'APPELS_ABOUTIS', 'DUREE_PHONING', 'RDV_OBTENUS'],
    universBesoin: ['ASV_NB', 'ASV_VP', 'ASV_MTT', 'VCC_NB', 'ASSUR_VIE'],
  },
  {
    code: 'AT_ASV',
    libelle: 'Atelier Assurance vie',
    activite: ['APPELS_PASSES', 'APPELS_ABOUTIS', 'RDV_OBTENUS'],
    universBesoin: ['ASV_NB', 'ASV_MTT', 'ASSUR_VIE'],
  },
  {
    code: 'AT_PREVOYANCE',
    libelle: 'Atelier Prévoyance',
    activite: ['APPELS_PASSES', 'RDV_OBTENUS'],
    universBesoin: ['PVC_PART', 'IARD_PART'],
  },
];
