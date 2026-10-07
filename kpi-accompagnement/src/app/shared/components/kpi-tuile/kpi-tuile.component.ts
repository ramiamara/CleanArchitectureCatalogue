import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TuileVm } from '../../../core/models/kpi.model';
import { PtsPipe } from '../../pipes/pts.pipe';
import { SignePipe } from '../../pipes/signe.pipe';

/** Pavé indicateur : libellé, +x pts, évolution du % d'atteinte. */
@Component({
  selector: 'app-kpi-tuile',
  imports: [PtsPipe, SignePipe],
  templateUrl: './kpi-tuile.component.html',
  styleUrl: './kpi-tuile.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class KpiTuileComponent {
  readonly tuile = input.required<TuileVm>();
}
