import { Pipe, PipeTransform } from '@angular/core';

/** 12 → "+12 pts" · -8 → "−8 pts" */
@Pipe({ name: 'pts' })
export class PtsPipe implements PipeTransform {
  transform(valeur: number | null | undefined, suffixe = ' pts'): string {
    if (valeur == null || Number.isNaN(valeur)) return '—';
    if (valeur === 0) return `0${suffixe}`;
    return `${valeur > 0 ? '+' : '−'}${Math.abs(valeur)}${suffixe}`;
  }
}
