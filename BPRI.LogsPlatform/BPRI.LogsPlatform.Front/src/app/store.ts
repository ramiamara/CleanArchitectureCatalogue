import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { catchError, forkJoin, of } from 'rxjs';
import { ApiService } from './api.service';
import { ProjectSummary, View } from './models';

export interface RangeOption { label: string; hours: number; }

export const RANGES: RangeOption[] = [
  { label: '1 heure', hours: 1 },
  { label: '6 heures', hours: 6 },
  { label: '24 heures', hours: 24 },
  { label: '7 jours', hours: 24 * 7 },
  { label: '30 jours', hours: 24 * 30 },
];

/** État global : vue, projet, application, période, TraceId recherché. */
@Injectable({ providedIn: 'root' })
export class Store {
  private readonly api = inject(ApiService);

  readonly view = signal<View>('exceptions');
  readonly projects = signal<ProjectSummary[]>([]);
  readonly project = signal<string>('CatalogueAPP');
  /** Nom de l'application ; vide = toutes les applications du projet. */
  readonly application = signal<string>('');
  readonly applications = signal<string[]>([]);
  readonly rangeHours = signal<number>(24);
  readonly traceId = signal<string>('');
  /** Incrémenté pour forcer un rechargement (bouton Actualiser). */
  readonly refreshTick = signal(0);
  readonly apiError = signal<string | null>(null);

  /** Fenêtre de temps courante ; recalculée à chaque changement / rafraîchissement. */
  readonly window = computed(() => {
    this.refreshTick();
    const to = new Date();
    const from = new Date(to.getTime() - this.rangeHours() * 3_600_000);
    return { from: from.toISOString(), to: to.toISOString() };
  });

  constructor() {
    this.api.projects().subscribe({
      next: list => {
        this.projects.set(list);
        // Le code par défaut est CatalogueAPP ; sinon on prend le premier projet connu.
        if (list.length && !list.some(p => p.code === this.project())) this.project.set(list[0].code);
        this.apiError.set(null);
      },
      error: () => this.apiError.set("Impossible de joindre l'API des logs (http://localhost:5080)."),
    });

    // Liste des applications du projet : celles qui ont des exceptions ou des requêtes tracées.
    effect(() => {
      const project = this.project();
      const { from, to } = this.window();
      forkJoin([
        this.api.filters(project, from, to).pipe(catchError(() => of(null))),
        this.api.requestFilters(project, from, to).pipe(catchError(() => of(null))),
      ]).subscribe(([exceptions, requests]) => {
        const names = new Set<string>([...(exceptions?.applications ?? []), ...(requests?.applications ?? [])]);
        this.applications.set([...names].sort());
      });
    });
  }

  setProject(code: string): void {
    this.project.set(code);
    this.application.set('');
  }

  goToTrace(traceId: string, view: View): void {
    this.traceId.set(traceId);
    this.view.set(view);
  }

  refresh(): void { this.refreshTick.update(v => v + 1); }
}
