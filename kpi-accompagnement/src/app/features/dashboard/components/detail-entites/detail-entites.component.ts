import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { ChartConfiguration } from 'chart.js';
import { LigneDetailVm } from '../../../../core/models/kpi.model';
import { ChartComponent } from '../../../../shared/components/chart/chart.component';
import { COLORS } from '../../../../shared/chart/chart-theme';
import { referenceLine } from '../../../../shared/chart/chart-plugins';
import { PtsPipe } from '../../../../shared/pipes/pts.pipe';

/** Tableau + graphique "Détail par agence / conseiller / animateur". */
@Component({
  selector: 'app-detail-entites',
  imports: [ChartComponent, PtsPipe],
  templateUrl: './detail-entites.component.html',
  styleUrl: './detail-entites.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DetailEntitesComponent {
  readonly lignes = input.required<LigneDetailVm[]>();
  /** "Conseiller", "Agence" ou "Animateur". */
  readonly libelleEntite = input.required<string>();
  readonly libelleTotal = input('Total');
  readonly pctTotal = input.required<number>();
  readonly ptsTotal = input.required<number>();

  protected readonly graphique = computed<ChartConfiguration<'bar'>>(() => {
    const lignes = this.lignes();
    return {
      type: 'bar',
      data: {
        labels: lignes.map((l) => l.entite.libelle),
        datasets: [{ label: '% indicateurs en progression', data: lignes.map((l) => l.pctProgression), backgroundColor: COLORS.navy, maxBarThickness: 40 }],
      },
      options: {
        plugins: { legend: { display: false }, tooltip: { callbacks: { label: (c) => `${c.parsed.y} % en progression` } } },
        scales: { y: { beginAtZero: true, max: 100, ticks: { callback: (v) => `${v} %` } }, x: { grid: { display: false } } },
      },
      plugins: [referenceLine(this.pctTotal(), `${this.libelleTotal()} ${this.pctTotal()} %`, COLORS.individuel)],
    };
  });
}
