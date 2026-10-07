import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/** "4/9 — Nombre d'indicateurs positifs" avec jauge. */
@Component({
  selector: 'app-score-ratio',
  templateUrl: './score-ratio.component.html',
  styleUrl: './score-ratio.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScoreRatioComponent {
  readonly positifs = input.required<number>();
  readonly total = input.required<number>();
  protected readonly pct = computed(() => (this.total() ? (this.positifs() / this.total()) * 100 : 0));
}
