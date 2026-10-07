import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { ChartConfiguration } from 'chart.js';
import { PilotageVm } from '../../../../core/models/kpi.model';
import { ChartComponent } from '../../../../shared/components/chart/chart.component';
import { COLORS } from '../../../../shared/chart/chart-theme';
import { insideBarLabels } from '../../../../shared/chart/chart-plugins';

/** Barre 100 % "Individuels / Collectifs". */
@Component({
  selector: 'app-repartition-chart',
  imports: [ChartComponent],
  templateUrl: './repartition-chart.component.html',
  styleUrl: './repartition-chart.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RepartitionChartComponent {
  readonly vm = input.required<PilotageVm>();

  protected readonly config = computed<ChartConfiguration<'bar'>>(() => {
    const { nbCoachings, nbParticipations, pctIndividuels, pctCollectifs } = this.vm();
    const nombres = [nbCoachings, nbParticipations];
    const barre = { borderSkipped: false as const, barPercentage: 1, categoryPercentage: 1 };
    return {
      type: 'bar',
      data: {
        labels: ['Répartition'],
        datasets: [
          { ...barre, label: 'Individuels', data: [pctIndividuels], backgroundColor: COLORS.individuel, borderRadius: { topLeft: 6, bottomLeft: 6 } },
          { ...barre, label: 'Collectifs', data: [pctCollectifs], backgroundColor: COLORS.collectif, borderRadius: { topRight: 6, bottomRight: 6 } },
        ],
      },
      options: {
        indexAxis: 'y',
        scales: { x: { stacked: true, max: 100, display: false }, y: { stacked: true, display: false } },
        plugins: {
          legend: { display: false },
          tooltip: { callbacks: { label: (c) => `${c.dataset.label} : ${nombres[c.datasetIndex]} (${c.parsed.x} %)` } },
        },
      },
      plugins: [insideBarLabels((pct, i) => `${nombres[i]} (${pct} %)`)],
    };
  });
}
