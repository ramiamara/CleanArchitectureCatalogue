import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';
import { ChartConfiguration } from 'chart.js';
import { BlocAtelierVm } from '../../../../core/models/kpi.model';
import { ChartComponent } from '../../../../shared/components/chart/chart.component';
import { ScoreRatioComponent } from '../../../../shared/components/score-ratio/score-ratio.component';
import { COLORS } from '../../../../shared/chart/chart-theme';
import { AtelierActiviteTableComponent } from '../atelier-activite-table/atelier-activite-table.component';
import { AtelierUniversTableComponent } from '../atelier-univers-table/atelier-univers-table.component';

/** Bloc "Atelier collectif <libellé>". */
@Component({
  selector: 'app-bloc-atelier',
  imports: [ChartComponent, ScoreRatioComponent, AtelierActiviteTableComponent, AtelierUniversTableComponent],
  templateUrl: './bloc-atelier.component.html',
  styleUrls: ['../bloc.scss', './bloc-atelier.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BlocAtelierComponent {
  readonly bloc = input.required<BlocAtelierVm>();
  protected readonly vue = signal<'tableaux' | 'graphique'>('tableaux');

  protected readonly graphiqueActivite = computed<ChartConfiguration<'bar'>>(() => {
    const lignes = this.bloc().activite;
    return {
      type: 'bar',
      data: {
        labels: lignes.map((l) => l.libelle),
        datasets: [
          { label: 'Référentiel', data: lignes.map((l) => l.referentiel), backgroundColor: '#c5d3e6' },
          { label: 'Jour J', data: lignes.map((l) => l.valeurJ), backgroundColor: lignes.map((l) => (l.pts >= 0 ? COLORS.positif : COLORS.negatif)) },
        ],
      },
      options: {
        plugins: { legend: { position: 'bottom' }, title: { display: true, text: 'Activité — valeur vs référentiel', align: 'start', color: COLORS.navy } },
        scales: { x: { grid: { display: false } }, y: { beginAtZero: true } },
      },
    };
  });

  protected readonly graphiqueUnivers = computed<ChartConfiguration<'bar'>>(() => {
    const lignes = this.bloc().universBesoin;
    return {
      type: 'bar',
      data: {
        labels: lignes.map((l) => l.libelle),
        datasets: [
          { label: 'Jour J', data: lignes.map((l) => l.valeurJ), backgroundColor: '#9ec9e8' },
          { label: 'J + 2 mois', data: lignes.map((l) => l.valeurJ2), backgroundColor: COLORS.collectif },
        ],
      },
      options: {
        plugins: {
          legend: { position: 'bottom' },
          title: { display: true, text: "Univers de besoin — % d'atteinte", align: 'start', color: COLORS.navy },
          tooltip: { callbacks: { label: (c) => `${c.dataset.label} : ${c.parsed.y} %` } },
        },
        scales: { x: { grid: { display: false } }, y: { beginAtZero: true, ticks: { callback: (v) => `${v} %` } } },
      },
    };
  });
}
