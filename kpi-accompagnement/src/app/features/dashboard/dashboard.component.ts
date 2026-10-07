import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { AuthService } from '../../core/services/auth.service';
import { KpiStoreService } from '../../core/services/kpi-store.service';
import { PerimetreService } from '../../core/services/perimetre.service';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { FriseCardComponent } from './components/frise-card/frise-card.component';
import { PilotageCardComponent } from './components/pilotage-card/pilotage-card.component';
import { ResultatsCardComponent } from './components/resultats-card/resultats-card.component';

/** Page KPI-xx-1 : même page pour tous les profils, le périmètre change selon l'utilisateur connecté. */
@Component({
  selector: 'app-dashboard',
  imports: [PageHeaderComponent, PilotageCardComponent, ResultatsCardComponent, FriseCardComponent],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardComponent {
  protected readonly auth = inject(AuthService);
  protected readonly store = inject(KpiStoreService);
  protected readonly perimetre = inject(PerimetreService);

  protected readonly codePage = computed(() => `KPI-${this.auth.config().code}-1`);
  protected readonly titre = computed(() => `KPI — Profil ${this.auth.config().libelle}`);
}
