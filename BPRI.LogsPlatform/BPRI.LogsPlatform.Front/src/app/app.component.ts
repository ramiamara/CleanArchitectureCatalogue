import { Component, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { DashboardComponent } from './dashboard/dashboard.component';
import { IconComponent } from './icon.component';
import { ICONS } from './icons';
import { RequestsComponent } from './requests/requests.component';
import { RANGES, StoreService } from './store.service';
import { ThemeService } from './theme.service';

@Component({
  selector: 'app-root',
  imports: [
    FormsModule, MatButtonModule, MatButtonToggleModule, MatFormFieldModule, MatInputModule, MatSelectModule,
    MatToolbarModule, MatTooltipModule, DashboardComponent, RequestsComponent, IconComponent,
  ],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
})
export class AppComponent {
  protected readonly store = inject(StoreService);
  protected readonly theme = inject(ThemeService);
  protected readonly ranges = RANGES;
  protected readonly icons = ICONS;
  protected readonly traceInput = signal('');

  constructor() {
    effect(() => this.traceInput.set(this.store.traceId()));
  }

  protected searchTrace(): void {
    this.store.traceId.set(this.traceInput().trim());
  }
}
