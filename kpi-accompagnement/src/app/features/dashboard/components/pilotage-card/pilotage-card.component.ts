import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { PilotageVm } from '../../../../core/models/kpi.model';
import { RepartitionChartComponent } from '../repartition-chart/repartition-chart.component';

/** Bloc "Pilotage des accompagnements individuels et collectifs". */
@Component({
  selector: 'app-pilotage-card',
  imports: [RouterLink, DecimalPipe, RepartitionChartComponent],
  templateUrl: './pilotage-card.component.html',
  styleUrl: './pilotage-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PilotageCardComponent {
  readonly vm = input.required<PilotageVm>();
  /** true pour le conseiller : on compte aussi les accompagnements en cours. */
  readonly avecEnCours = input(false);
}
