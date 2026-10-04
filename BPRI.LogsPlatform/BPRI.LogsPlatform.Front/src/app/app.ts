import { Component, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Dashboard } from './dashboard/dashboard';
import { Icon } from './icon';
import { ICONS } from './icons';
import { Requests } from './requests/requests';
import { RANGES, Store } from './store';
import { ThemeService } from './theme.service';

@Component({
  selector: 'app-root',
  
  imports: [
    FormsModule, MatButtonModule, MatButtonToggleModule, MatFormFieldModule, MatInputModule, MatSelectModule,
    MatToolbarModule, MatTooltipModule, Dashboard, Requests, Icon,
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  protected readonly store = inject(Store);
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

  protected clearTrace(): void {
    this.traceInput.set('');
    this.store.traceId.set('');
  }
}
