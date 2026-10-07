import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { Coaching } from '../../../../core/models/accompagnement.model';
import { KpiDataService } from '../../../../core/services/kpi-data.service';
import { DateFrPipe } from '../../../../shared/pipes/date-fr.pipe';
import { PtsPipe } from '../../../../shared/pipes/pts.pipe';

/** Bandeau d'informations du coaching (conseiller, agence, DA, DS, animateur, dates...). */
@Component({
  selector: 'app-coaching-infos',
  imports: [DateFrPipe, PtsPipe],
  templateUrl: './coaching-infos.component.html',
  styleUrl: './coaching-infos.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CoachingInfosComponent {
  private readonly data = inject(KpiDataService);

  readonly coaching = input.required<Coaching>();
  readonly positifs = input.required<number>();
  readonly total = input.required<number>();
  readonly ptsMoyen = input.required<number>();

  protected readonly conseiller = computed(() => this.data.collaborateur(this.coaching().collaborateurId));
  protected readonly agence = computed(() => this.data.agence(this.conseiller()?.agenceId ?? ''));
  protected readonly secteur = computed(() => this.data.secteur(this.agence()?.secteurId ?? ''));
  protected readonly animateur = computed(() => this.data.animateur(this.coaching().animateurId));
}
