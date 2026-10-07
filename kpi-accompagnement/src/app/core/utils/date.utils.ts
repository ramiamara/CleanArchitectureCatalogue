/** Helpers de dates sur des chaînes ISO (yyyy-mm-dd), sans dépendance externe. */

export function toDate(iso: string): Date {
  const [y, m, d] = iso.split('-').map(Number);
  return new Date(Date.UTC(y, m - 1, d));
}

export function toIso(d: Date): string {
  return d.toISOString().slice(0, 10);
}

export function addDays(iso: string, days: number): string {
  const d = toDate(iso);
  d.setUTCDate(d.getUTCDate() + days);
  return toIso(d);
}

export function addMonths(iso: string, months: number): string {
  const d = toDate(iso);
  d.setUTCMonth(d.getUTCMonth() + months);
  return toIso(d);
}

/** Bornes incluses. */
export function isBetween(iso: string, debut: string, fin: string): boolean {
  return iso >= debut && iso <= fin;
}

export function formatFr(iso: string): string {
  const [y, m, d] = iso.split('-');
  return `${d}/${m}/${y}`;
}

export function formatCourt(iso: string): string {
  const [, m, d] = iso.split('-');
  return `${d}/${m}`;
}

const MOIS = ['Janv.', 'Févr.', 'Mars', 'Avr.', 'Mai', 'Juin', 'Juil.', 'Août', 'Sept.', 'Oct.', 'Nov.', 'Déc.'];

export function moisLibelle(iso: string): string {
  const [y, m] = iso.split('-');
  return `${MOIS[Number(m) - 1]} ${y.slice(2)}`;
}

/** Liste des mois (yyyy-mm) couverts par une période. */
export function moisEntre(debut: string, fin: string): string[] {
  const res: string[] = [];
  let cur = debut.slice(0, 7) + '-01';
  while (cur.slice(0, 7) <= fin.slice(0, 7)) {
    res.push(cur.slice(0, 7));
    cur = addMonths(cur, 1);
  }
  return res;
}
