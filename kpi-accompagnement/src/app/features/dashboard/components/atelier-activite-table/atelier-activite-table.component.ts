import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { LigneActiviteAtelierVm } from '../../../../core/models/kpi.model';
import { DateFrPipe } from '../../../../shared/pipes/date-fr.pipe';
import { PtsPipe } from '../../../../shared/pipes/pts.pipe';
import { SignePipe } from '../../../../shared/pipes/signe.pipe';

/** Tableau "Activité" d'un atelier : valeur au jour J vs référentiel. */
@Component({
  selector: 'app-atelier-activite-table',
  imports: [DateFrPipe, PtsPipe, SignePipe],
  templateUrl: './atelier-activite-table.component.html',
  styleUrl: '../atelier-table.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AtelierActiviteTableComponent {
  readonly lignes = input.required<LigneActiviteAtelierVm[]>();
  readonly dateJ = input.required<string>();
}
