import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { KpiStoreService } from '../../core/services/kpi-store.service';
import { PerimetreService } from '../../core/services/perimetre.service';
import { DateFrPipe } from '../../shared/pipes/date-fr.pipe';

@Component({
  selector: 'app-header',
  imports: [RouterLink, DateFrPipe],
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HeaderComponent {
  protected readonly auth = inject(AuthService);
  protected readonly store = inject(KpiStoreService);
  protected readonly perimetre = inject(PerimetreService);
  private readonly router = inject(Router);

  protected deconnecter(): void {
    this.auth.deconnecter();
    this.store.reinitialiserFiltres();
    this.router.navigate(['/connexion']);
  }
}
