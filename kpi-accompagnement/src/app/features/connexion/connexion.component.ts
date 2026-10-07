import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { PROFILS, Profil } from '../../core/models/organisation.model';
import { AuthService } from '../../core/services/auth.service';
import { KpiStoreService } from '../../core/services/kpi-store.service';

/** Écran de connexion simulé : on choisit un compte de démo, son profil détermine ce qu'il voit. */
@Component({
  selector: 'app-connexion',
  templateUrl: './connexion.component.html',
  styleUrl: './connexion.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConnexionComponent {
  protected readonly auth = inject(AuthService);
  private readonly store = inject(KpiStoreService);
  private readonly router = inject(Router);

  protected readonly profils = PROFILS;

  protected readonly descriptions: Record<Profil, string> = {
    CONSEILLER: 'Ses propres coachings et ateliers',
    DA: 'Les conseillers de son agence · détail par conseiller',
    DS: 'Les agences de son secteur · détail par agence',
    AN: 'Les conseillers qu’il a animés · détail par conseiller',
    RA: 'Les animateurs de son équipe · détail par animateur',
  };

  protected connecter(utilisateurId: string): void {
    this.auth.connecter(utilisateurId);
    this.store.reinitialiserFiltres();
    this.router.navigate(['/']);
  }
}
