import { Pipe, PipeTransform } from '@angular/core';

/** Classe CSS selon le % d'atteinte d'objectif : ≥ 100 vert, ≥ 80 orange, sinon rouge. */
@Pipe({ name: 'atteinte' })
export class AtteintePipe implements PipeTransform {
  transform(pct: number): string {
    return pct >= 100 ? 'pos' : pct >= 80 ? 'warn' : 'neg';
  }
}
