import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { Sort, MatSortModule } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { ApiService } from '../api.service';
import { ChartComponent } from '../chart.component';
import { ChartThemeService } from '../chart-theme.service';
import { DetailDialogComponent } from '../detail-dialog.component';
import { SEVERITY_COLORS, severityLabel, shortType, statusColor, timelineLabel } from '../format';
import { IconComponent } from '../icon.component';
import { ICONS } from '../icons';
import { Filters, LogListItem, Severity, Stats, TraceTarget } from '../models';
import { StoreService } from '../store.service';

@Component({
  selector: 'app-dashboard',
  imports: [
    DatePipe, DecimalPipe, FormsModule, MatButtonModule, MatCardModule, MatFormFieldModule, MatInputModule,
    MatPaginatorModule, MatProgressBarModule, MatSelectModule, MatSortModule, MatTableModule, ChartComponent, IconComponent,
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: '../page.scss',
})
export class DashboardComponent {
  private readonly api = inject(ApiService);
  private readonly dialog = inject(MatDialog);
  protected readonly store = inject(StoreService);
  private readonly chartTheme = inject(ChartThemeService);

  protected readonly icons = ICONS;

  // ---- données
  protected readonly stats = signal<Stats | null>(null);
  protected readonly filters = signal<Filters>({ exceptionTypes: [], statusCodes: [], applications: [] });
  protected readonly rows = signal<LogListItem[]>([]);
  protected readonly total = signal(0);
  protected readonly loadingStats = signal(false);
  protected readonly loadingLogs = signal(false);

  // ---- filtres / pagination
  protected readonly severityFilter = signal<Severity[]>([]);
  protected readonly statusFilter = signal<number[]>([]);
  protected readonly typeFilter = signal<string | null>(null);
  protected readonly searchText = signal('');
  protected readonly page = signal(1);
  protected readonly pageSize = signal(25);
  protected readonly sortField = signal('occurredAtUtc');
  protected readonly sortDir = signal<'asc' | 'desc'>('desc');

  protected readonly severityOptions: Severity[] = ['Error', 'Warning', 'Information'];
  protected readonly columns = ['occurredAtUtc', 'severity', 'statusCode', 'exceptionType', 'message', 'request', 'traceId'];

  protected readonly label = severityLabel;
  protected readonly short = shortType;
  protected readonly color = statusColor;

  private statsToken = 0;
  private logsToken = 0;

  constructor() {
    // « Réinitialiser » de la barre du haut : on vide aussi les filtres du tableau.
    effect(() => {
      this.store.resetCount();
      untracked(() => this.resetFilters());
    });

    // Stats + valeurs de filtres : dépendent du projet, de l'application et de la période.
    effect(() => {
      const project = this.store.project();
      const application = this.store.application();
      const { from, to } = this.store.window();
      if (!project) return;
      const token = ++this.statsToken;
      this.loadingStats.set(true);
      this.api.stats(project, from, to, application).subscribe({
        next: s => { if (token === this.statsToken) { this.stats.set(s); this.loadingStats.set(false); } },
        error: () => { if (token === this.statsToken) { this.stats.set(null); this.loadingStats.set(false); } },
      });
      this.api.filters(project, from, to).subscribe({
        next: f => { if (token === this.statsToken) this.filters.set(f); },
        error: () => undefined,
      });
    });

    // Changement de projet / application / période / filtre => retour page 1
    effect(() => {
      this.store.project(); this.store.application(); this.store.rangeHours(); this.store.traceId();
      this.severityFilter(); this.statusFilter(); this.typeFilter(); this.searchText();
      this.page.set(1);
    });

    // Liste des logs
    effect(() => {
      const project = this.store.project();
      const application = this.store.application();
      const traceId = this.store.traceId();
      const { from, to } = this.store.listWindow();
      if (!project) return;
      const token = ++this.logsToken;
      this.loadingLogs.set(true);
      this.api.logs({
        project, application: application || undefined, from, to,
        severity: this.severityFilter(),
        statusCode: this.statusFilter(),
        exceptionType: this.typeFilter() ?? undefined,
        traceId: traceId || undefined,
        search: this.searchText().trim() || undefined,
        page: this.page(), pageSize: this.pageSize(),
        sortField: this.sortField(), sortDir: this.sortDir(),
      }).subscribe({
        next: r => {
          if (token !== this.logsToken) return;
          this.rows.set(r.items); this.total.set(r.total); this.loadingLogs.set(false);
          // Un TraceId qui ne correspond qu'à un log => on ouvre directement le détail.
          if (traceId && r.total === 1) this.open(r.items[0]);
        },
        error: () => { if (token === this.logsToken) { this.rows.set([]); this.total.set(0); this.loadingLogs.set(false); } },
      });
    });
  }

  protected readonly topEndpoints = computed(() => this.stats()?.topEndpoints ?? []);

  // ---- KPI
  protected readonly kpis = computed(() => {
    const s = this.stats();
    return [
      { label: 'Total', value: s?.total ?? 0, color: 'var(--app-primary)' },
      { label: 'Erreurs', value: s?.errors ?? 0, color: SEVERITY_COLORS.Error },
      { label: 'Avertissements', value: s?.warnings ?? 0, color: SEVERITY_COLORS.Warning },
      { label: 'Informations', value: s?.informations ?? 0, color: SEVERITY_COLORS.Information },
    ];
  });

  // ---- graphiques (Chart.js)
  private readonly palette = this.chartTheme.palette;

  protected readonly severityData = computed(() => {
    const s = this.stats();
    const order: Severity[] = ['Error', 'Warning', 'Information'];
    return {
      labels: ['Error', 'Warning', 'Info'],
      datasets: [{
        data: order.map(k => s?.bySeverity.find(x => x.severity === k)?.count ?? 0),
        backgroundColor: order.map(k => SEVERITY_COLORS[k]), borderColor: this.palette().border, borderWidth: 2,
      }],
    };
  });

  protected readonly statusData = computed(() => {
    const list = [...(this.stats()?.byStatusCode ?? [])].sort((a, b) => a.statusCode - b.statusCode);
    return {
      labels: list.map(x => String(x.statusCode)),
      datasets: [{ label: 'Occurrences', data: list.map(x => x.count), backgroundColor: list.map(x => statusColor(x.statusCode)), borderRadius: 6 }],
    };
  });

  protected readonly typeData = computed(() => {
    const list = (this.stats()?.byExceptionType ?? []).slice(0, 8);
    return {
      labels: list.map(x => shortType(x.exceptionType)),
      datasets: [{ label: 'Occurrences', data: list.map(x => x.count), backgroundColor: '#6366f1', borderRadius: 6 }],
    };
  });

  protected readonly timelineData = computed(() => {
    const s = this.stats();
    const pts = s?.timeline ?? [];
    return {
      labels: pts.map(x => timelineLabel(x.timestampUtc, s?.timelineBucket)),
      datasets: [
        { label: 'Error', data: pts.map(x => x.error), backgroundColor: SEVERITY_COLORS.Error, borderRadius: 3 },
        { label: 'Warning', data: pts.map(x => x.warning), backgroundColor: SEVERITY_COLORS.Warning, borderRadius: 3 },
        { label: 'Info', data: pts.map(x => x.information), backgroundColor: SEVERITY_COLORS.Information, borderRadius: 3 },
      ],
    };
  });

  protected readonly doughnutOptions = this.chartTheme.doughnut;
  protected readonly barOptions = this.chartTheme.bar;
  protected readonly horizontalBarOptions = this.chartTheme.horizontalBar;
  protected readonly timelineOptions = this.chartTheme.stacked;

  // ---- tableau
  protected onPage(e: PageEvent): void {
    this.pageSize.set(e.pageSize);
    this.page.set(e.pageIndex + 1);
  }

  protected onSort(e: Sort): void {
    this.sortField.set(e.direction ? e.active : 'occurredAtUtc');
    this.sortDir.set(e.direction === 'asc' ? 'asc' : 'desc');
  }

  protected resetFilters(): void {
    this.severityFilter.set([]); this.statusFilter.set([]); this.typeFilter.set(null); this.searchText.set('');
  }

  protected readonly hasFilters = computed(() =>
    this.severityFilter().length > 0 || this.statusFilter().length > 0 || !!this.typeFilter() || !!this.searchText());

  // ---- détail
  protected open(row: LogListItem): void {
    this.dialog.open<DetailDialogComponent, LogListItem, TraceTarget | undefined>(DetailDialogComponent, {
      data: row,
      width: 'min(46rem, 100vw)', maxWidth: '100vw', height: '100vh',
      position: { right: '0', top: '0' }, panelClass: 'detail-panel',
    }).afterClosed().subscribe(target => { if (target) this.store.goToTrace(target.traceId, target.view); });
  }
}
