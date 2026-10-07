import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { ChartConfiguration } from 'chart.js';
import { LIBELLE_AXE } from '../../core/models/organisation.model';
import { AuthService } from '../../core/services/auth.service';
import { KpiStoreService } from '../../core/services/kpi-store.service';
import { moisLibelle } from '../../core/utils/date.utils';
import { ChartComponent } from '../../shared/components/chart/chart.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { COLORS } from '../../shared/chart/chart-theme';

/** Page KPI-xx-02 : "Zoom vers inducteurs" du pilotage. */
@Component({
  selector: 'app-inducteurs',
  imports: [ChartComponent, PageHeaderComponent],
  templateUrl: './inducteurs.component.html',
  styleUrl: './inducteurs.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InducteursComponent {
  protected readonly store = inject(KpiStoreService);
  private readonly auth = inject(AuthService);

  protected readonly codePage = computed(() => `KPI-${this.auth.config().code}-02`);
  protected readonly libelleEntite = computed(() => {
    const axe = this.auth.config().axeDetail;
    return (axe ? LIBELLE_AXE[axe] : 'Conseiller').toLowerCase();
  });

  protected readonly jhParMois = computed<ChartConfiguration<'bar'>>(() => {
    const d = this.store.inducteurs()?.jhParMois ?? [];
    return {
      type: 'bar',
      data: {
        labels: d.map((m) => moisLibelle(`${m.mois}-01`)),
        datasets: [
          { label: 'Coaching individuel', data: d.map((m) => m.coaching), backgroundColor: COLORS.individuel },
          { label: 'Atelier collectif', data: d.map((m) => m.atelier), backgroundColor: COLORS.collectif },
        ],
      },
      options: {
        plugins: { legend: { position: 'bottom' }, tooltip: { mode: 'index' } },
        scales: { x: { stacked: true, grid: { display: false } }, y: { stacked: true, beginAtZero: true, title: { display: true, text: 'J/H' } } },
      },
    };
  });

  protected readonly hauteurTypologie = computed(() => `${Math.max(200, (this.store.inducteurs()?.parTypologie.length ?? 0) * 34 + 40)}px`);
  protected readonly parTypologie = computed<ChartConfiguration<'bar'>>(() => {
    const d = [...(this.store.inducteurs()?.parTypologie ?? [])].sort((a, b) => b.nb - a.nb);
    return {
      type: 'bar',
      data: {
        labels: d.map((x) => x.libelle),
        datasets: [{ label: 'Accompagnements', data: d.map((x) => x.nb), backgroundColor: d.map((x) => (x.estAtelier ? COLORS.collectif : COLORS.individuel)), maxBarThickness: 22 }],
      },
      options: { indexAxis: 'y', plugins: { legend: { display: false } }, scales: { x: { beginAtZero: true, ticks: { precision: 0 } }, y: { grid: { display: false } } } },
    };
  });

  protected readonly parDuree = computed<ChartConfiguration<'doughnut'>>(() => {
    const d = this.store.inducteurs();
    return {
      type: 'doughnut',
      data: { labels: ['Court', 'Long'], datasets: [{ data: [d?.court ?? 0, d?.long ?? 0], backgroundColor: ['#8fb3d9', COLORS.navy], borderColor: '#fff', borderWidth: 2 }] },
      options: { cutout: '62%', plugins: { legend: { position: 'bottom' } } },
    };
  });

  protected readonly hauteurEntite = computed(() => `${Math.max(200, (this.store.inducteurs()?.jhParEntite.length ?? 0) * 34 + 60)}px`);
  protected readonly jhParEntite = computed<ChartConfiguration<'bar'>>(() => {
    const d = this.store.inducteurs()?.jhParEntite ?? [];
    return {
      type: 'bar',
      data: {
        labels: d.map((x) => x.libelle),
        datasets: [
          { label: 'Coaching individuel', data: d.map((x) => x.coaching), backgroundColor: COLORS.individuel, maxBarThickness: 22 },
          { label: 'Atelier collectif', data: d.map((x) => x.atelier), backgroundColor: COLORS.collectif, maxBarThickness: 22 },
        ],
      },
      options: {
        indexAxis: 'y',
        plugins: { legend: { position: 'bottom' } },
        scales: { x: { stacked: true, beginAtZero: true, title: { display: true, text: 'J/H' } }, y: { stacked: true, grid: { display: false } } },
      },
    };
  });
}
