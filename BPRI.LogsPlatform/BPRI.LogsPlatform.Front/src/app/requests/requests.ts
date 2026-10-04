import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, computed, effect, inject, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { ApiService } from '../api.service';
import { ChartView } from '../chart';
import { STATUS_CLASS_COLORS, durationClass, formatDuration, methodColor, statusColor } from '../format';
import { Icon } from '../icon';
import { ICONS } from '../icons';
import { RequestFilters, RequestListItem, RequestStats, TraceTarget } from '../models';
import { Store } from '../store';
import { ThemeService } from '../theme.service';
import { RequestDetailDialog } from './request-detail-dialog';

@Component({
  selector: 'app-requests',
  
  imports: [
    DatePipe, DecimalPipe, FormsModule, MatButtonModule, MatCardModule, MatFormFieldModule, MatInputModule,
    MatPaginatorModule, MatProgressBarModule, MatSelectModule, MatSortModule, MatTableModule, ChartView, Icon,
  ],
  templateUrl: './requests.html',
  styleUrl: '../dashboard/dashboard.scss',
})
export class Requests {
  private readonly api = inject(ApiService);
  private readonly dialog = inject(MatDialog);
  protected readonly store = inject(Store);
  private readonly theme = inject(ThemeService);

  readonly clearTrace = output<void>();
  protected readonly icons = ICONS;

  // ---- données
  protected readonly stats = signal<RequestStats | null>(null);
  protected readonly filters = signal<RequestFilters>({ applications: [], methods: [], statusCodes: [] });
  protected readonly rows = signal<RequestListItem[]>([]);
  protected readonly total = signal(0);
  protected readonly loadingStats = signal(false);
  protected readonly loadingList = signal(false);

  // ---- filtres / pagination
  protected readonly methodFilter = signal<string[]>([]);
  protected readonly statusFilter = signal<number[]>([]);
  protected readonly minDuration = signal<number | null>(null);
  protected readonly searchText = signal('');
  protected readonly page = signal(1);
  protected readonly pageSize = signal(25);
  protected readonly sortField = signal('occurredAtUtc');
  protected readonly sortDir = signal<'asc' | 'desc'>('desc');

  protected readonly columns = ['occurredAtUtc', 'method', 'path', 'statusCode', 'durationMs', 'userName', 'applicationName', 'traceId'];

  protected readonly color = statusColor;
  protected readonly colorOfMethod = methodColor;
  protected readonly format = formatDuration;
  protected readonly duration = durationClass;

  private statsToken = 0;
  private listToken = 0;

  constructor() {
    // Stats + valeurs de filtres : dépendent du projet, de l'application et de la période.
    effect(() => {
      const project = this.store.project();
      const application = this.store.application();
      const { from, to } = this.store.window();
      const token = ++this.statsToken;
      this.loadingStats.set(true);
      this.api.requestStats(project, from, to, application).subscribe({
        next: s => { if (token === this.statsToken) { this.stats.set(s); this.loadingStats.set(false); } },
        error: () => { if (token === this.statsToken) { this.stats.set(null); this.loadingStats.set(false); } },
      });
      this.api.requestFilters(project, from, to).subscribe({
        next: f => { if (token === this.statsToken) this.filters.set(f); },
        error: () => undefined,
      });
    });

    // Changement de projet / application / période / filtre => retour page 1
    effect(() => {
      this.store.project(); this.store.application(); this.store.rangeHours(); this.store.traceId();
      this.methodFilter(); this.statusFilter(); this.minDuration(); this.searchText();
      this.page.set(1);
    });

    // Liste des requêtes
    effect(() => {
      const project = this.store.project();
      const application = this.store.application();
      const traceId = this.store.traceId();
      const w = this.store.window();
      // Une recherche par TraceId ignore la période (on remonte au maximum autorisé : 365 jours).
      const from = traceId ? new Date(Date.now() - 365 * 86_400_000).toISOString() : w.from;
      const token = ++this.listToken;
      this.loadingList.set(true);
      this.api.requests({
        project, application: application || undefined, from, to: w.to,
        method: this.methodFilter(),
        statusCode: this.statusFilter(),
        minDurationMs: this.minDuration() ?? undefined,
        traceId: traceId || undefined,
        search: this.searchText().trim() || undefined,
        page: this.page(), pageSize: this.pageSize(),
        sortField: this.sortField(), sortDir: this.sortDir(),
      }).subscribe({
        next: r => {
          if (token !== this.listToken) return;
          this.rows.set(r.items); this.total.set(r.total); this.loadingList.set(false);
          // Un TraceId qui ne correspond qu'à une requête => on ouvre directement le détail.
          if (traceId && r.total === 1) this.open(r.items[0]);
        },
        error: () => { if (token === this.listToken) { this.rows.set([]); this.total.set(0); this.loadingList.set(false); } },
      });
    });
  }

  protected readonly topEndpoints = computed(() => this.stats()?.topEndpoints ?? []);
  protected readonly slowEndpoints = computed(() => this.stats()?.slowEndpoints ?? []);

  protected readonly minDurationText = computed(() => String(this.minDuration() ?? ''));

  // ---- KPI
  protected readonly kpis = computed(() => {
    const s = this.stats();
    return [
      { label: 'Requêtes', value: `${(s?.total ?? 0).toLocaleString('fr-FR')}`, color: 'var(--app-primary)' },
      { label: 'Erreurs client (4xx)', value: `${(s?.clientErrors ?? 0).toLocaleString('fr-FR')}`, color: STATUS_CLASS_COLORS['4xx'] },
      { label: 'Erreurs serveur (5xx)', value: `${(s?.serverErrors ?? 0).toLocaleString('fr-FR')}`, color: STATUS_CLASS_COLORS['5xx'] },
      { label: 'Durée moyenne', value: formatDuration(s?.avgDurationMs ?? 0), color: '#8b5cf6' },
      { label: 'Durée P95', value: formatDuration(s?.p95DurationMs ?? 0), color: '#06b6d4' },
    ];
  });

  // ---- graphiques (Chart.js)
  private readonly palette = computed(() =>
    this.theme.mode() === 'dark'
      ? { text: '#cbd5e1', grid: 'rgba(148,163,184,.18)', border: '#1c1d24' }
      : { text: '#475569', grid: 'rgba(100,116,139,.18)', border: '#ffffff' });

  protected readonly classData = computed(() => {
    const list = this.stats()?.byStatusClass ?? [];
    return {
      labels: list.map(x => x.statusClass),
      datasets: [{
        data: list.map(x => x.count),
        backgroundColor: list.map(x => STATUS_CLASS_COLORS[x.statusClass] ?? '#64748b'),
        borderColor: this.palette().border, borderWidth: 2,
      }],
    };
  });

  protected readonly methodData = computed(() => {
    const list = this.stats()?.byMethod ?? [];
    return {
      labels: list.map(x => x.method),
      datasets: [{
        data: list.map(x => x.count),
        backgroundColor: list.map(x => methodColor(x.method)),
        borderColor: this.palette().border, borderWidth: 2,
      }],
    };
  });

  protected readonly statusData = computed(() => {
    const list = [...(this.stats()?.byStatusCode ?? [])].sort((a, b) => a.statusCode - b.statusCode);
    return {
      labels: list.map(x => String(x.statusCode)),
      datasets: [{ label: 'Requêtes', data: list.map(x => x.count), backgroundColor: list.map(x => statusColor(x.statusCode)), borderRadius: 6 }],
    };
  });

  protected readonly timelineData = computed(() => {
    const s = this.stats();
    const pts = s?.timeline ?? [];
    const day = s?.timelineBucket === 'day';
    const p = (n: number) => String(n).padStart(2, '0');
    const fmt = (iso: string) => {
      const d = new Date(iso);
      return day ? `${p(d.getDate())}/${p(d.getMonth() + 1)}` : `${p(d.getHours())}:${p(d.getMinutes())}`;
    };
    return {
      labels: pts.map(x => fmt(x.timestampUtc)),
      datasets: [
        { type: 'bar', label: '2xx / 3xx', data: pts.map(x => x.success), backgroundColor: STATUS_CLASS_COLORS['2xx'], borderRadius: 3, yAxisID: 'y', order: 2 },
        { type: 'bar', label: '4xx', data: pts.map(x => x.clientError), backgroundColor: STATUS_CLASS_COLORS['4xx'], borderRadius: 3, yAxisID: 'y', order: 2 },
        { type: 'bar', label: '5xx', data: pts.map(x => x.serverError), backgroundColor: STATUS_CLASS_COLORS['5xx'], borderRadius: 3, yAxisID: 'y', order: 2 },
        { type: 'line', label: 'Durée moyenne (ms)', data: pts.map(x => x.avgDurationMs), borderColor: '#8b5cf6', backgroundColor: '#8b5cf6', borderWidth: 2, pointRadius: 0, tension: 0.3, yAxisID: 'y1', order: 1 },
      ],
    };
  });

  private legend() {
    return { position: 'bottom' as const, labels: { color: this.palette().text, usePointStyle: true } };
  }

  protected readonly doughnutOptions = computed(() => ({ cutout: '62%', plugins: { legend: this.legend() } }));

  protected readonly barOptions = computed(() => {
    const c = this.palette();
    return {
      plugins: { legend: { display: false } },
      scales: {
        x: { grid: { display: false }, ticks: { color: c.text } },
        y: { beginAtZero: true, grid: { color: c.grid }, ticks: { color: c.text, precision: 0 } },
      },
    };
  });

  protected readonly timelineOptions = computed(() => {
    const c = this.palette();
    return {
      interaction: { mode: 'index' as const, intersect: false },
      plugins: { legend: this.legend() },
      scales: {
        x: { stacked: true, grid: { display: false }, ticks: { color: c.text, maxRotation: 0, autoSkip: true } },
        y: { stacked: true, beginAtZero: true, grid: { color: c.grid }, ticks: { color: c.text, precision: 0 } },
        y1: { position: 'right' as const, beginAtZero: true, grid: { drawOnChartArea: false }, ticks: { color: c.text } },
      },
    };
  });

  // ---- tableau
  protected onPage(e: PageEvent): void {
    this.pageSize.set(e.pageSize);
    this.page.set(e.pageIndex + 1);
  }

  protected onSort(e: Sort): void {
    this.sortField.set(e.direction ? e.active : 'occurredAtUtc');
    this.sortDir.set(e.direction === 'asc' ? 'asc' : 'desc');
  }

  protected setMinDuration(value: string): void {
    const n = Number(value);
    this.minDuration.set(value && n > 0 ? n : null);
  }

  protected resetFilters(): void {
    this.methodFilter.set([]); this.statusFilter.set([]); this.minDuration.set(null); this.searchText.set('');
  }

  protected readonly hasFilters = computed(() =>
    this.methodFilter().length > 0 || this.statusFilter().length > 0 || this.minDuration() !== null || !!this.searchText());

  // ---- détail
  protected open(row: RequestListItem): void {
    this.dialog.open<RequestDetailDialog, RequestListItem, TraceTarget | undefined>(RequestDetailDialog, {
      data: row,
      width: 'min(46rem, 100vw)', maxWidth: '100vw', height: '100vh',
      position: { right: '0', top: '0' }, panelClass: 'detail-panel',
    }).afterClosed().subscribe(target => { if (target) this.store.goToTrace(target.traceId, target.view); });
  }
}
