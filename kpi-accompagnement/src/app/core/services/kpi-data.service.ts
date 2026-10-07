import { Injectable, computed } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { delay, of } from 'rxjs';
import { Atelier, Coaching } from '../models/accompagnement.model';
import { Agence, Animateur, Collaborateur, Secteur } from '../models/organisation.model';
import { AGENCES, ANIMATEURS, COLLABORATEURS, SECTEURS } from '../mocks/organisation.mock';
import { ATELIERS_MOCK, COACHINGS_MOCK } from '../mocks/accompagnements.mock';

export interface KpiDataset {
  secteurs: Secteur[];
  agences: Agence[];
  animateurs: Animateur[];
  collaborateurs: Collaborateur[];
  coachings: Coaching[];
  ateliers: Atelier[];
}

/**
 * Source de données (mock).
 * Pour brancher l'API : remplacer `of(...)` par `this.http.get<KpiDataset>('/api/kpi')`.
 */
@Injectable({ providedIn: 'root' })
export class KpiDataService {
  private readonly dataset = toSignal(
    of<KpiDataset>({
      secteurs: SECTEURS,
      agences: AGENCES,
      animateurs: ANIMATEURS,
      collaborateurs: COLLABORATEURS,
      coachings: COACHINGS_MOCK,
      ateliers: ATELIERS_MOCK,
    }).pipe(delay(200)),
  );

  readonly charge = computed(() => !!this.dataset());
  readonly coachings = computed(() => this.dataset()?.coachings ?? []);
  readonly ateliers = computed(() => this.dataset()?.ateliers ?? []);
  readonly collaborateurs = computed(() => this.dataset()?.collaborateurs ?? []);

  collaborateur(id: string): Collaborateur | undefined {
    return this.dataset()?.collaborateurs.find((c) => c.id === id);
  }

  agence(id: string): Agence | undefined {
    return this.dataset()?.agences.find((a) => a.id === id);
  }

  secteur(id: string): Secteur | undefined {
    return this.dataset()?.secteurs.find((s) => s.id === id);
  }

  animateur(id: string): Animateur | undefined {
    return this.dataset()?.animateurs.find((a) => a.id === id);
  }

  coaching(id: string): Coaching | undefined {
    return this.dataset()?.coachings.find((c) => c.id === id);
  }

  nomCourt(collaborateurId: string): string {
    const c = this.collaborateur(collaborateurId);
    return c ? `${c.prenom} ${c.nom.charAt(0)}.` : collaborateurId;
  }
}
