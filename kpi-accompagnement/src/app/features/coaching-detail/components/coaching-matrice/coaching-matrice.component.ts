import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { Coaching } from '../../../../core/models/accompagnement.model';
import { TuileVm } from '../../../../core/models/kpi.model';
import { INDICATEURS_BY_CODE } from '../../../../core/mocks/referentiels.mock';
import { AtteintePipe } from '../../../../shared/pipes/atteinte.pipe';
import { DateFrPipe } from '../../../../shared/pipes/date-fr.pipe';
import { PtsPipe } from '../../../../shared/pipes/pts.pipe';
import { SignePipe } from '../../../../shared/pipes/signe.pipe';

export interface LigneMatrice {
  code: string;
  progression: TuileVm;
}

export interface GroupeMatrice {
  titre: string;
  lignes: LigneMatrice[];
}

/** Matrice KPI × journées de coaching : réalisé / objectif et % d'atteinte. */
@Component({
  selector: 'app-coaching-matrice',
  imports: [DateFrPipe, PtsPipe, SignePipe, AtteintePipe],
  templateUrl: './coaching-matrice.component.html',
  styleUrl: './coaching-matrice.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CoachingMatriceComponent {
  readonly coaching = input.required<Coaching>();
  readonly groupes = input.required<GroupeMatrice[]>();

  protected readonly libelles = INDICATEURS_BY_CODE;
  protected readonly sessions = computed(() => this.coaching().sessions);

  /** Index de la 1ère et de la dernière journée réalisée (bornes de la progression). */
  private readonly bornes = computed(() => {
    const idx = this.sessions().flatMap((s, i) => (s.realisee ? [i] : []));
    return { debut: idx[0] ?? -1, fin: idx.at(-1) ?? -1 };
  });

  protected estBorne(i: number): boolean {
    return i === this.bornes().debut || i === this.bornes().fin;
  }

  protected libelleBorne(i: number): string {
    return i === this.bornes().debut ? 'Début' : i === this.bornes().fin ? 'Fin' : '';
  }

  protected pct(realise: number, objectif: number): number {
    return objectif ? Math.round((realise / objectif) * 10000) / 100 : 0;
  }
}
