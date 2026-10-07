import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';
import { ChartConfiguration } from 'chart.js';
import { FriseItemVm, KpiFiltres } from '../../../../core/models/kpi.model';
import { formatFr, toDate } from '../../../../core/utils/date.utils';
import { ChartComponent } from '../../../../shared/components/chart/chart.component';
import { periodeBand } from '../../../../shared/chart/chart-plugins';

const JOUR = 86_400_000;
const MOIS = ['janv.', 'févr.', 'mars', 'avr.', 'mai', 'juin', 'juil.', 'août', 'sept.', 'oct.', 'nov.', 'déc.'];
const ts = (iso: string) => toDate(iso).getTime();
const COULEUR = { resultats: '#1f8f4e', pilotage: '#8fb3d9', exclu: '#cfd6df' };

/** Frise : quels coachings sont pris en compte pour la période filtrée ? */
@Component({
  selector: 'app-frise-card',
  imports: [ChartComponent],
  templateUrl: './frise-card.component.html',
  styleUrl: './frise-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FriseCardComponent {
  readonly items = input.required<FriseItemVm[]>();
  readonly filtres = input.required<KpiFiltres>();
  readonly avecEnCours = input(false);

  protected readonly ouvert = signal(true);
  protected readonly couleur = COULEUR;
  protected readonly hauteur = computed(() => `${Math.max(160, this.items().length * 26 + 70)}px`);

  protected readonly graphique = computed<ChartConfiguration<'bar'>>(() => {
    const items = this.items();
    const { dateDebut, dateFin } = this.filtres();
    const dates = items.flatMap((i) => [ts(i.coaching.dateDebut), ts(i.coaching.dateFin)]).concat(ts(dateDebut), ts(dateFin));
    const couleur = (i: FriseItemVm) => (i.inclusResultats ? COULEUR.resultats : i.inclusPilotage ? COULEUR.pilotage : COULEUR.exclu);

    return {
      type: 'bar',
      data: {
        labels: items.map((i) => i.libelle),
        datasets: [
          {
            label: 'Coaching',
            data: items.map((i) => [ts(i.coaching.dateDebut), ts(i.coaching.dateFin)] as [number, number]),
            backgroundColor: items.map(couleur),
            borderRadius: 4,
            borderSkipped: false,
            barPercentage: 0.7,
          },
        ],
      },
      options: {
        indexAxis: 'y',
        scales: {
          x: {
            type: 'linear',
            min: Math.min(...dates) - 15 * JOUR,
            max: Math.max(...dates) + 15 * JOUR,
            ticks: {
              stepSize: 30.44 * JOUR,
              callback: (v) => {
                const d = new Date(Number(v));
                return `${MOIS[d.getUTCMonth()]} ${String(d.getUTCFullYear()).slice(2)}`;
              },
            },
          },
          y: { grid: { display: false } },
        },
        plugins: {
          legend: { display: false },
          tooltip: {
            callbacks: {
              label: (c) => {
                const i = items[c.dataIndex];
                const etat = i.inclusResultats ? 'pris en compte' : i.inclusPilotage ? 'pilotage uniquement' : 'exclu';
                return [`${formatFr(i.coaching.dateDebut)} → ${formatFr(i.coaching.dateFin)}`, `${i.coaching.statut === 'TERMINE' ? 'Terminé' : 'En cours'} · ${etat}`];
              },
            },
          },
        },
      },
      plugins: [periodeBand(ts(dateDebut), ts(dateFin), 'Période filtrée')],
    };
  });
}
