import { Severity } from './models';

export const SEVERITY_COLORS: Record<Severity, string> = {
  Error: '#ef4444',
  Warning: '#f59e0b',
  Information: '#3b82f6',
};

export const STATUS_CLASS_COLORS: Record<string, string> = {
  '2xx': '#22c55e',
  '3xx': '#06b6d4',
  '4xx': '#f59e0b',
  '5xx': '#ef4444',
};

const METHOD_COLORS: Record<string, string> = {
  GET: '#3b82f6',
  POST: '#22c55e',
  PUT: '#f59e0b',
  PATCH: '#8b5cf6',
  DELETE: '#ef4444',
};

export const methodColor = (method: string): string => METHOD_COLORS[method.toUpperCase()] ?? '#64748b';

export const severityLabel = (s: Severity): string => (s === 'Information' ? 'Info' : s);

/** Couleur d'un code HTTP : 5xx rouge, 403 violet, 401 orange, 404 bleu, autres 4xx ambre, 2xx/3xx vert. */
export function statusColor(code: number): string {
  if (code >= 500) return '#ef4444';
  if (code === 403) return '#8b5cf6';
  if (code === 401) return '#f97316';
  if (code === 404) return '#3b82f6';
  if (code >= 400) return '#f59e0b';
  return '#22c55e';
}

export const shortType = (t: string): string => t.split('.').pop() ?? t;

export function formatDuration(ms: number | null | undefined): string {
  if (ms === null || ms === undefined) return '—';
  return ms < 1000 ? `${ms} ms` : `${(ms / 1000).toFixed(2)} s`;
}

export function durationClass(ms: number | null | undefined): string {
  if (!ms) return '';
  if (ms >= 3000) return 'bad';
  return ms >= 1000 ? 'warn' : '';
}

/** Met en forme un corps JSON ; renvoie le texte tel quel s'il n'est pas du JSON. */
export function prettyBody(text: string | null | undefined): string {
  if (!text) return '';
  try {
    return JSON.stringify(JSON.parse(text), null, 2);
  } catch {
    return text;
  }
}
