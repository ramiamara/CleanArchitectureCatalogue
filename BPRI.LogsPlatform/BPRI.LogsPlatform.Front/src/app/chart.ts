import { Component, ElementRef, OnDestroy, effect, input, viewChild } from '@angular/core';
import { Chart, ChartData, ChartOptions, ChartType, registerables } from 'chart.js';

Chart.register(...registerables);

/** Enveloppe minimale autour de Chart.js (recrée le graphique quand données ou options changent). */
@Component({
  selector: 'app-chart',
  
  template: `<canvas #canvas></canvas>`,
  styles: `:host { display: block; position: relative; height: 100%; width: 100%; }`,
})
export class ChartView implements OnDestroy {
  readonly type = input.required<ChartType>();
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  readonly data = input.required<ChartData<any>>();
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  readonly options = input<ChartOptions<any>>({});
  private readonly canvas = viewChild<ElementRef<HTMLCanvasElement>>('canvas');
  private chart?: Chart;

  constructor() {
    effect(() => {
      const canvas = this.canvas();
      const type = this.type(), data = this.data(), options = this.options();
      if (!canvas) return;
      this.chart?.destroy();
      this.chart = new Chart(canvas.nativeElement, { type, data, options: { responsive: true, maintainAspectRatio: false, ...options } });
    });
  }

  ngOnDestroy(): void { this.chart?.destroy(); }
}
