import { Severity } from './models';

export const SEVERITY_COLORS: Record<Severity, string> = {
  Error: '#ef4444',
  Warning: '#f59e0b',
  Information: '#3b82f6',
};

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
