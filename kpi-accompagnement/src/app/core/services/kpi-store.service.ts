import { Injectable, computed, inject, signal } from '@angular/core';
import { KpiFiltres, Perimetre } from '../models/kpi.model';
import { AuthService } from './auth.service';
import { KpiCalculService } from './kpi-calcul.service';
import { KpiDataService } from './kpi-data.service';
import { PerimetreService } from './perimetre.service';

export const FILTRES_DEFAUT: KpiFiltres = {
  dateDebut: '2026-01-01',
  dateFin: '2026-12-31',
  metier: 'TOUS',
  entiteId: 'TOUS',
  type: 'TOUS',
  typologie: 'TOUS',
  duree: 'TOUS',
};

/** État de l'écran KPI : filtres + view-models recalculés automatiquement (signals). */
@Injectable({ providedIn: 'root' })
export class KpiStoreService {
  private readonly auth = inject(AuthService);
  private readonly data = inject(KpiDataService);
  private readonly perimetreService = inject(PerimetreService);
  private readonly calcul = inject(KpiCalculService);

  readonly filtres = signal<KpiFiltres>({ ...FILTRES_DEFAUT });
  readonly filtresOuverts = signal(false);

  readonly nbFiltresActifs = computed(() => {
    const f = this.filtres();
    return (Object.keys(FILTRES_DEFAUT) as (keyof KpiFiltres)[]).filter((k) => f[k] !== FILTRES_DEFAUT[k]).length;
  });

  private readonly perimetre = computed(() => (this.data.charge() ? this.perimetreService.construire(this.filtres()) : null));
  private readonly avecEnCours = computed(() => this.auth.config().pilotageAvecEnCours);

  readonly pilotage = computed(() => this.calculer((p) => this.calcul.pilotage(p, this.avecEnCours())));
  readonly resultatIndividuel = computed(() => this.calculer((p) => this.calcul.resultatIndividuel(p)));
  readonly resultatCollectif = computed(() => this.calculer((p) => this.calcul.resultatCollectif(p)));
  readonly frise = computed(() => this.calculer((p) => this.calcul.frise(p, this.avecEnCours())) ?? []);
  readonly inducteurs = computed(() => this.calculer((p) => this.calcul.inducteurs(p, this.avecEnCours())));

  appliquerFiltres(filtres: KpiFiltres): void {
    this.filtres.set(filtres);
  }

  reinitialiserFiltres(): void {
    this.filtres.set({ ...FILTRES_DEFAUT });
  }

  private calculer<T>(fn: (p: Perimetre) => T): T | null {
    const p = this.perimetre();
    return p ? fn(p) : null;
  }
}
