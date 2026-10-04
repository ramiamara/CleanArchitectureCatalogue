import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { ApiService } from '../api.service';
import { durationClass, formatDuration, prettyBody, statusColor } from '../format';
import { Icon } from '../icon';
import { ICONS } from '../icons';
import { RequestDetail, RequestListItem, TraceTarget } from '../models';

@Component({
  selector: 'app-request-detail-dialog',
  
  imports: [DatePipe, MatButtonModule, MatDialogModule, Icon],
  template: `
    <div class="head">
      <h2 mat-dialog-title>Détail de la requête</h2>
      <button mat-icon-button aria-label="Fermer" (click)="ref.close()"><app-icon [path]="icons.close" /></button>
    </div>
    <mat-dialog-content>
      @if (d(); as d) {
        <div class="title">
          @if (d.statusCode) { <span class="status" [style.--c]="color(d.statusCode)">{{ d.statusCode }}</span> }
          <strong>{{ d.httpMethod }} {{ d.path }}</strong>
        </div>
        <div class="trace">
          <code>{{ d.traceId }}</code>
          <button mat-icon-button aria-label="Copier le TraceId" (click)="copy(d.traceId)">
            <app-icon [path]="copied() ? icons.check : icons.copy" />
          </button>
          <button mat-button (click)="go(d.traceId, 'requests')"><app-icon [path]="icons.search" /> Toutes les requêtes de ce TraceId</button>
          <button mat-button (click)="go(d.traceId, 'exceptions')"><app-icon [path]="icons.search" /> Logs et exceptions associés</button>
        </div>
        <dl>
          <dt>Date (UTC)</dt><dd>{{ d.occurredAtUtc | date: 'dd/MM/yyyy HH:mm:ss' : 'UTC' }}</dd>
          <dt>Durée</dt><dd><span [class]="duration(d.durationMs)">{{ format(d.durationMs) }}</span></dd>
          <dt>Cprj</dt><dd>{{ d.cprj }}</dd>
          <dt>Application</dt><dd>{{ d.applicationName }}</dd>
          <dt>Query string</dt><dd><code>{{ d.queryString || '—' }}</code></dd>
          <dt>Utilisateur</dt><dd>{{ d.userName || '—' }} @if (d.userId) { <code>({{ d.userId }})</code> }</dd>
          <dt>Environnement</dt><dd>{{ d.environmentName || '—' }}</dd>
          <dt>Machine</dt><dd>{{ d.machineName || '—' }}</dd>
        </dl>
        <h4>Corps de la requête</h4>
        @if (d.requestBody) { <pre>{{ pretty(d.requestBody) }}</pre> } @else { <p class="none">Aucun corps enregistré.</p> }
        <h4>Corps de la réponse</h4>
        @if (d.responseBody) { <pre>{{ pretty(d.responseBody) }}</pre> } @else { <p class="none">Aucun corps enregistré.</p> }
      }
    </mat-dialog-content>
  `,
  styles: `
    .head { display: flex; align-items: center; justify-content: space-between; padding-right: .75rem; }
    .title { display: flex; align-items: center; gap: .6rem; font-size: 1.1rem; word-break: break-all; }
    .trace { display: flex; align-items: center; flex-wrap: wrap; gap: .25rem; margin-top: .5rem; code { font-size: .8rem; } }
    dl { display: grid; grid-template-columns: 9rem 1fr; gap: .35rem .75rem; margin: 1rem 0;
      dt { color: var(--app-on-variant); } dd { margin: 0; word-break: break-all; } }
    code, pre { font-family: ui-monospace, Consolas, monospace; }
    pre { margin: 0 0 1rem; padding: .9rem; border-radius: 12px; overflow: auto; max-height: 22rem; font-size: .78rem; line-height: 1.5;
      background: var(--app-container); border: 1px solid var(--app-outline); }
    h4 { margin: 1rem 0 .4rem; }
    .none { color: var(--app-on-variant); margin: 0 0 1rem; }
    .warn { color: #f59e0b; font-weight: 700; } .bad { color: #ef4444; font-weight: 700; }
    .status { padding: .1rem .5rem; border-radius: 6px; font-weight: 700; font-size: .8rem; color: var(--c);
      background: color-mix(in srgb, var(--c) 16%, transparent); border: 1px solid color-mix(in srgb, var(--c) 45%, transparent); }
  `,
})
export class RequestDetailDialog {
  protected readonly ref = inject<MatDialogRef<RequestDetailDialog, TraceTarget | undefined>>(MatDialogRef);
  private readonly row = inject<RequestListItem>(MAT_DIALOG_DATA);
  private readonly api = inject(ApiService);
  protected readonly icons = ICONS;
  protected readonly d = signal<RequestDetail | null>(this.row as RequestDetail);
  protected readonly copied = signal(false);
  protected readonly color = statusColor;
  protected readonly format = formatDuration;
  protected readonly duration = durationClass;
  protected readonly pretty = prettyBody;

  constructor() {
    this.api.request(this.row.id).subscribe({ next: x => this.d.set(x), error: () => undefined });
  }

  protected go(traceId: string, view: TraceTarget['view']): void {
    this.ref.close({ traceId, view });
  }

  protected async copy(text: string): Promise<void> {
    try {
      await navigator.clipboard.writeText(text);
      this.copied.set(true);
      setTimeout(() => this.copied.set(false), 1500);
    } catch { /* presse-papiers indisponible */ }
  }
}
