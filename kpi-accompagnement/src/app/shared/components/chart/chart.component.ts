import { ChangeDetectionStrategy, Component, DestroyRef, ElementRef, effect, inject, input, viewChild } from '@angular/core';
import { Chart, ChartConfiguration, registerables } from 'chart.js';
import { appliquerThemeChart } from '../../chart/chart-theme';

Chart.register(...registerables);
appliquerThemeChart();

/** Wrapper Chart.js : le graphique se met à jour quand `config` change. */
@Component({
  selector: 'app-chart',
  templateUrl: './chart.component.html',
  styleUrl: './chart.component.scss',
  host: { '[style.height]': 'hauteur()' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChartComponent {
  readonly config = input.required<ChartConfiguration>();
  readonly hauteur = input('260px');
  readonly description = input('Graphique');

  private readonly canvas = viewChild.required<ElementRef<HTMLCanvasElement>>('canvas');
  private chart?: Chart;
  private typeCourant?: string;

  constructor() {
    effect(() => this.dessiner(this.config()));
    inject(DestroyRef).onDestroy(() => this.chart?.destroy());
  }

  private dessiner(config: ChartConfiguration): void {
    const options = { responsive: true, maintainAspectRatio: false, ...config.options };
    // Mise à jour simple si même type et pas de plugin inline (ceux-ci capturent leurs paramètres)
    if (this.chart && this.typeCourant === config.type && !config.plugins?.length) {
      this.chart.data = config.data;
      this.chart.options = options;
      this.chart.update();
      return;
    }
    this.chart?.destroy();
    this.chart = new Chart(this.canvas().nativeElement, { ...config, options });
    this.typeCourant = config.type;
  }
}
