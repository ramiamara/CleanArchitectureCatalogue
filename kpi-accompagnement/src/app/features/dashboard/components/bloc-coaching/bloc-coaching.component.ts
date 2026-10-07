import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ChartConfiguration } from 'chart.js';
import { BlocCoachingVm } from '../../../../core/models/kpi.model';
import { KpiDataService } from '../../../../core/services/kpi-data.service';
import { formatCourt } from '../../../../core/utils/date.utils';
import { ChartComponent } from '../../../../shared/components/chart/chart.component';
import { KpiTuileComponent } from '../../../../shared/components/kpi-tuile/kpi-tuile.component';
import { ScoreRatioComponent } from '../../../../shared/components/score-ratio/score-ratio.component';
import { COLORS } from '../../../../shared/chart/chart-theme';
import { referenceLine } from '../../../../shared/chart/chart-plugins';

/** Bloc "Accompagnement individuel <libellé>" : Activité + Univers de besoin. */
@Component({
  selector: 'app-bloc-coaching',
  imports: [RouterLink, ChartComponent, KpiTuileComponent, ScoreRatioComponent],
  templateUrl: './bloc-coaching.component.html',
  styleUrls: ['../bloc.scss', './bloc-coaching.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BlocCoachingComponent {
  private readonly data = inject(KpiDataService);

  readonly bloc = input.required<BlocCoachingVm>();
  /** Affiche la liste des coachings (profils managers). */
  readonly afficherCoachings = input(false);

  protected readonly vue = signal<'paves' | 'graphique'>('paves');

  protected readonly liens = computed(() =>
    this.bloc().coachings.map((c) => ({
      id: c.id,
      libelle: `${this.data.nomCourt(c.collaborateurId)} · ${formatCourt(c.dateDebut)} → ${formatCourt(c.dateFin)}`,
    })),
  );

  protected readonly graphique = computed<ChartConfiguration<'bar'>>(() => {
    const tuiles = [...this.bloc().activite, ...this.bloc().universBesoin];
    return {
      type: 'bar',
      data: {
        labels: tuiles.map((t) => t.libelle),
        datasets: [
          { label: '% atteinte début', data: tuiles.map((t) => t.pctDebut), backgroundColor: '#c5d3e6' },
          { label: '% atteinte fin', data: tuiles.map((t) => t.pctFin), backgroundColor: tuiles.map((t) => (t.pts >= 0 ? COLORS.positif : COLORS.negatif)) },
        ],
      },
      options: {
        plugins: { legend: { position: 'bottom' }, tooltip: { callbacks: { label: (c) => `${c.dataset.label} : ${c.parsed.y} %` } } },
        scales: { y: { beginAtZero: true, ticks: { callback: (v) => `${v} %` } }, x: { grid: { display: false } } },
      },
      plugins: [referenceLine(100, 'Objectif 100 %', COLORS.navy)],
    };
  });
}
