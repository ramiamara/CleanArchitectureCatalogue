import { Injectable, computed, inject } from '@angular/core';
import { ThemeService } from './theme.service';

/** Couleurs et options Chart.js communes aux pages Exceptions et Requêtes (suivent le thème clair / sombre). */
@Injectable({ providedIn: 'root' })
export class ChartThemeService {
  private readonly theme = inject(ThemeService);

  readonly palette = computed(() => {
    if (this.theme.mode() === 'dark') {
      return { text: '#cbd5e1', grid: 'rgba(148,163,184,.18)', border: '#1c1d24' };
    }
    return { text: '#475569', grid: 'rgba(100,116,139,.18)', border: '#ffffff' };
  });

  readonly doughnut = computed(() => ({ cutout: '62%', plugins: { legend: this.legend() } }));

  readonly bar = computed(() => ({ plugins: { legend: { display: false } }, scales: this.axes(false) }));

  readonly horizontalBar = computed(() => ({
    indexAxis: 'y' as const,
    plugins: { legend: { display: false } },
    scales: this.axes(false, true),
  }));

  /** Barres empilées (chronologie). */
  readonly stacked = computed(() => ({
    interaction: { mode: 'index' as const, intersect: false },
    plugins: { legend: this.legend() },
    scales: this.axes(true),
  }));

  /** Barres empilées + courbe sur un second axe à droite (chronologie des requêtes et durée moyenne). */
  readonly stackedWithLine = computed(() => {
    const stacked = this.stacked();
    return {
      ...stacked,
      scales: {
        ...stacked.scales,
        y1: {
          position: 'right' as const,
          beginAtZero: true,
          grid: { drawOnChartArea: false },
          ticks: { color: this.palette().text },
        },
      },
    };
  });

  private legend() {
    return { position: 'bottom' as const, labels: { color: this.palette().text, usePointStyle: true } };
  }

  private axes(stacked: boolean, horizontal = false) {
    const c = this.palette();
    const category = { stacked, grid: { display: false }, ticks: { color: c.text, maxRotation: 0, autoSkip: true } };
    const value = { stacked, beginAtZero: true, grid: { color: c.grid }, ticks: { color: c.text, precision: 0 } };
    return horizontal ? { x: value, y: category } : { x: category, y: value };
  }
}
