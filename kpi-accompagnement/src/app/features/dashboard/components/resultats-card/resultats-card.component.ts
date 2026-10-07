import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { LIBELLE_AXE, Profil } from '../../../../core/models/organisation.model';
import { AuthService } from '../../../../core/services/auth.service';
import { KpiStoreService } from '../../../../core/services/kpi-store.service';
import { BlocAtelierComponent } from '../bloc-atelier/bloc-atelier.component';
import { BlocCoachingComponent } from '../bloc-coaching/bloc-coaching.component';
import { DetailEntitesComponent } from '../detail-entites/detail-entites.component';
import { SyntheseResultatsComponent } from '../synthese-resultats/synthese-resultats.component';

const LIBELLE_TOTAL: Record<Profil, string> = {
  CONSEILLER: 'Total',
  DA: 'Agence',
  DS: 'Secteur',
  AN: 'Total animé',
  RA: 'Équipe',
};

/** Bloc "Résultats" : onglets individuel / collectif. */
@Component({
  selector: 'app-resultats-card',
  imports: [SyntheseResultatsComponent, BlocCoachingComponent, BlocAtelierComponent, DetailEntitesComponent],
  templateUrl: './resultats-card.component.html',
  styleUrl: './resultats-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResultatsCardComponent {
  protected readonly store = inject(KpiStoreService);
  protected readonly auth = inject(AuthService);

  protected readonly onglet = signal<'individuel' | 'collectif'>('individuel');

  protected readonly estConseiller = computed(() => this.auth.profil() === 'CONSEILLER');
  protected readonly libelleEntite = computed(() => {
    const axe = this.auth.config().axeDetail;
    return axe ? LIBELLE_AXE[axe] : null;
  });
  protected readonly libelleTotal = computed(() => LIBELLE_TOTAL[this.auth.profil()]);
  protected readonly regleCalcul = computed(() =>
    this.estConseiller() ? "Nb d'indicateurs positifs / nb total d'indicateurs" : 'Moyenne des % de chaque conseiller',
  );

  /** Synthèse de l'onglet courant. */
  protected readonly synthese = computed(() =>
    this.onglet() === 'individuel' ? this.store.resultatIndividuel() : this.store.resultatCollectif(),
  );

  constructor() {
    // si le filtre "type" masque l'onglet courant, on bascule
    effect(() => {
      const type = this.store.filtres().type;
      if (type === 'ATELIER') this.onglet.set('collectif');
      if (type === 'COACHING') this.onglet.set('individuel');
    });
  }
}
