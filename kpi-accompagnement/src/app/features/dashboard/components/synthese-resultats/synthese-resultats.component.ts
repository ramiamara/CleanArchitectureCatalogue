import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { ChartConfiguration } from 'chart.js';
import { ChartComponent } from '../../../../shared/components/chart/chart.component';
import { COLORS } from '../../../../shared/chart/chart-theme';
import { PtsPipe } from '../../../../shared/pipes/pts.pipe';

/** "XX % indicateurs en progression" + "+X pts en moyenne". */
@Component({
  selector: 'app-synthese-resultats',
  imports: [ChartComponent, PtsPipe],
  templateUrl: './synthese-resultats.component.html',
  styleUrl: './synthese-resultats.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SyntheseResultatsComponent {
  readonly pctProgression = input.required<number>();
  readonly ptsMoyen = input.required<number>();
  readonly regleCalcul = input('');

  protected readonly donut = computed<ChartConfiguration<'doughnut'>>(() => ({
    type: 'doughnut',
    data: { datasets: [{ data: [this.pctProgression(), 100 - this.pctProgression()], backgroundColor: [COLORS.positif, '#e3e8ef'], borderWidth: 0 }] },
    options: { cutout: '74%', plugins: { legend: { display: false }, tooltip: { enabled: false } } },
  }));
}
