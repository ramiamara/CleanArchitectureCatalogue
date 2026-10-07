import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { LigneUniversAtelierVm } from '../../../../core/models/kpi.model';
import { DateFrPipe } from '../../../../shared/pipes/date-fr.pipe';
import { PtsPipe } from '../../../../shared/pipes/pts.pipe';
import { SignePipe } from '../../../../shared/pipes/signe.pipe';

/** Tableau "Univers de besoin" d'un atelier : % d'atteinte au jour J et à J+2 mois. */
@Component({
  selector: 'app-atelier-univers-table',
  imports: [DateFrPipe, PtsPipe, SignePipe],
  templateUrl: './atelier-univers-table.component.html',
  styleUrl: '../atelier-table.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AtelierUniversTableComponent {
  readonly lignes = input.required<LigneUniversAtelierVm[]>();
  readonly dateJ = input.required<string>();
  readonly dateJ2 = input.required<string>();
}
