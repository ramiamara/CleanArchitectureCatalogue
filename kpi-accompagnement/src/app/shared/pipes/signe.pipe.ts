import { Pipe, PipeTransform } from '@angular/core';

/** Classe CSS selon le signe d'une progression : pos / neg / zero. */
@Pipe({ name: 'signe' })
export class SignePipe implements PipeTransform {
  transform(valeur: number): string {
    return valeur > 0 ? 'pos' : valeur < 0 ? 'neg' : 'zero';
  }
}
