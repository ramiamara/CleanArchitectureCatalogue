import { Pipe, PipeTransform } from '@angular/core';
import { formatFr } from '../../core/utils/date.utils';

/** "2026-03-01" → "01/03/2026" */
@Pipe({ name: 'dateFr' })
export class DateFrPipe implements PipeTransform {
  transform(iso: string | null | undefined): string {
    return iso ? formatFr(iso) : '—';
  }
}
