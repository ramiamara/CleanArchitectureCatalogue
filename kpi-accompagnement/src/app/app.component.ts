import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AuthService } from './core/services/auth.service';
import { KpiDataService } from './core/services/kpi-data.service';
import { HeaderComponent } from './layout/header/header.component';
import { FiltresPanelComponent } from './layout/filtres-panel/filtres-panel.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, HeaderComponent, FiltresPanelComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppComponent {
  protected readonly auth = inject(AuthService);
  protected readonly data = inject(KpiDataService);
}
