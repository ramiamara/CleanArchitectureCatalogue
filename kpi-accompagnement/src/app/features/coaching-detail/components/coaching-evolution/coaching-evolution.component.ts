import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { ChartConfiguration } from 'chart.js';
import { Coaching } from '../../../../core/models/accompagnement.model';
import { INDICATEURS_BY_CODE } from '../../../../core/mocks/referentiels.mock';
import { formatFr } from '../../../../core/utils/date.utils';
import { ChartComponent } from '../../../../shared/components/chart/chart.component';
import { COLORS } from '../../../../shared/chart/chart-theme';
import { referenceLine } from '../../../../shared/chart/chart-plugins';
import { GroupeMatrice } from '../coaching-matrice/coaching-matrice.component';

/** Courbes d'évolution du % d'atteinte par indicateur. */
@Component({
  selector: 'app-coaching-evolution',
  imports: [ChartComponent],
  templateUrl: './coaching-evolution.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CoachingEvolutionComponent {
  readonly coaching = input.required<Coaching>();
  readonly groupes = input.required<GroupeMatrice[]>();

  protected readonly graphique = computed<ChartConfiguration<'line'>>(() => {
    const sessions = this.coaching().sessions.filter((s) => s.realisee);
    const codes = this.groupes().flatMap((g) => g.lignes.map((l) => l.code));
    return {
      type: 'line',
      data: {
        labels: sessions.map((s) => formatFr(s.date)),
        datasets: codes.map((code, i) => ({
          label: INDICATEURS_BY_CODE[code].libelle,
          data: sessions.map((s) => Math.round((s.mesures[code].realise / s.mesures[code].objectif) * 1000) / 10),
          borderColor: COLORS.series[i % COLORS.series.length],
          backgroundColor: COLORS.series[i % COLORS.series.length],
          tension: 0.25,
          pointRadius: 3,
          borderWidth: 2,
          hidden: i >= 5, // 5 courbes affichées par défaut, les autres via la légende
        })),
      },
      options: {
        interaction: { mode: 'index', intersect: false },
        plugins: { legend: { position: 'right' }, tooltip: { callbacks: { label: (c) => `${c.dataset.label} : ${c.parsed.y} %` } } },
        scales: { y: { beginAtZero: true, ticks: { callback: (v) => `${v} %` } }, x: { grid: { display: false } } },
      },
      plugins: [referenceLine(100, 'Objectif', COLORS.individuel)],
    };
  });
}
