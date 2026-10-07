import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { KpiFiltres } from '../../core/models/kpi.model';
import { LIBELLE_AXE } from '../../core/models/organisation.model';
import { TYPOLOGIES_ATELIER, TYPOLOGIES_COACHING } from '../../core/mocks/referentiels.mock';
import { AuthService } from '../../core/services/auth.service';
import { KpiStoreService } from '../../core/services/kpi-store.service';
import { PerimetreService } from '../../core/services/perimetre.service';

/** Tiroir "FILTRES" : on modifie un brouillon, appliqué au clic sur "Appliquer". */
@Component({
  selector: 'app-filtres-panel',
  templateUrl: './filtres-panel.component.html',
  styleUrl: './filtres-panel.component.scss',
  host: { '(document:keydown.escape)': 'fermer()' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FiltresPanelComponent {
  protected readonly store = inject(KpiStoreService);
  protected readonly auth = inject(AuthService);
  protected readonly perimetre = inject(PerimetreService);

  protected readonly brouillon = signal<KpiFiltres>(this.store.filtres());
  protected readonly typologiesCoaching = TYPOLOGIES_COACHING;
  protected readonly typologiesAtelier = TYPOLOGIES_ATELIER;

  protected readonly libelleEntite = computed(() => {
    const axe = this.auth.config().axeDetail;
    return axe ? LIBELLE_AXE[axe] : null;
  });

  protected readonly periodeInvalide = computed(() => this.brouillon().dateDebut > this.brouillon().dateFin);

  constructor() {
    // à chaque ouverture, on repart des filtres appliqués
    effect(() => {
      if (this.store.filtresOuverts()) this.brouillon.set({ ...this.store.filtres() });
    });
  }

  protected modifier(changes: Partial<KpiFiltres>): void {
    this.brouillon.update((f) => {
      const suivant = { ...f, ...changes };
      // une typologie de coaching n'a pas de sens si on filtre sur les ateliers (et inversement)
      const estAtelier = suivant.typologie.startsWith('AT_');
      if ((suivant.type === 'COACHING' && estAtelier) || (suivant.type === 'ATELIER' && !estAtelier && suivant.typologie !== 'TOUS')) {
        suivant.typologie = 'TOUS';
      }
      return suivant;
    });
  }

  protected valeur(event: Event): string {
    return (event.target as HTMLInputElement | HTMLSelectElement).value;
  }

  protected appliquer(): void {
    if (this.periodeInvalide()) return;
    this.store.appliquerFiltres(this.brouillon());
    this.fermer();
  }

  protected reinitialiser(): void {
    this.store.reinitialiserFiltres();
    this.brouillon.set({ ...this.store.filtres() });
  }

  protected fermer(): void {
    this.store.filtresOuverts.set(false);
  }
}
