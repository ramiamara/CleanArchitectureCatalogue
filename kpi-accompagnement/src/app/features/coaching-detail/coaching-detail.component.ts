import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { Router } from '@angular/router';
import { TYPOLOGIES_COACHING } from '../../core/mocks/referentiels.mock';
import { AuthService } from '../../core/services/auth.service';
import { KpiCalculService } from '../../core/services/kpi-calcul.service';
import { KpiDataService } from '../../core/services/kpi-data.service';
import { KpiStoreService } from '../../core/services/kpi-store.service';
import { PerimetreService } from '../../core/services/perimetre.service';
import { formatCourt } from '../../core/utils/date.utils';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { CoachingEvolutionComponent } from './components/coaching-evolution/coaching-evolution.component';
import { CoachingInfosComponent } from './components/coaching-infos/coaching-infos.component';
import { CoachingMatriceComponent, LigneMatrice } from './components/coaching-matrice/coaching-matrice.component';

/** Page KPI-xx-03 : matrice d'accompagnement d'un coaching. */
@Component({
  selector: 'app-coaching-detail',
  imports: [PageHeaderComponent, CoachingInfosComponent, CoachingEvolutionComponent, CoachingMatriceComponent],
  templateUrl: './coaching-detail.component.html',
  styleUrl: './coaching-detail.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CoachingDetailComponent {
  private readonly auth = inject(AuthService);
  private readonly data = inject(KpiDataService);
  private readonly store = inject(KpiStoreService);
  private readonly perimetre = inject(PerimetreService);
  private readonly calcul = inject(KpiCalculService);
  private readonly router = inject(Router);

  /** Paramètre de route ":id". */
  readonly id = input.required<string>();

  protected readonly codePage = computed(() => `KPI-${this.auth.config().code}-03`);

  /** Coaching affiché, seulement s'il est dans le périmètre de l'utilisateur. */
  protected readonly coaching = computed(() => {
    const c = this.data.coaching(this.id());
    return c && this.perimetre.peutVoir(c) ? c : undefined;
  });

  protected readonly typologie = computed(() => TYPOLOGIES_COACHING.find((t) => t.code === this.coaching()?.typologie));

  /** Autres coachings de même libellé visibles (pour le sélecteur). */
  protected readonly autres = computed(() =>
    this.store
      .frise()
      .filter((f) => f.coaching.typologie === this.coaching()?.typologie)
      .map((f) => ({
        id: f.coaching.id,
        libelle: `${this.data.nomCourt(f.coaching.collaborateurId)} · ${formatCourt(f.coaching.dateDebut)} → ${formatCourt(f.coaching.dateFin)}`,
      })),
  );

  /** Lignes de la matrice, groupées Activité / Univers de besoin. */
  protected readonly groupes = computed(() => {
    const c = this.coaching();
    const t = this.typologie();
    if (!c || !t) return [];
    const ligne = (code: string): LigneMatrice => ({ code, progression: this.calcul.progression(c, code) });
    return [
      { titre: 'Activité', lignes: t.activite.map(ligne) },
      { titre: 'Univers de besoin', lignes: t.universBesoin.map(ligne) },
    ];
  });

  protected readonly synthese = computed(() => {
    const pts = this.groupes().flatMap((g) => g.lignes.map((l) => l.progression.pts));
    const positifs = pts.filter((v) => v > 0);
    return {
      positifs: positifs.length,
      total: pts.length,
      ptsMoyen: positifs.length ? Math.round(positifs.reduce((a, b) => a + b, 0) / positifs.length) : 0,
    };
  });

  protected changerCoaching(event: Event): void {
    this.router.navigate(['/coaching', (event.target as HTMLSelectElement).value]);
  }
}
