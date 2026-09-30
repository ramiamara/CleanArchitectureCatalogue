import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { ApiService } from './api.service';
import { Icon } from './icon';
import { ICONS } from './icons';
import { LogDetail, LogListItem } from './models';
import { severityLabel, shortType, statusColor } from './format';

@Component({
  selector: 'app-detail-dialog',
  imports: [DatePipe, MatButtonModule, MatDialogModule, Icon],
  template: `
    <div class="head">
      <h2 mat-dialog-title>Détail du log</h2>
      <button mat-icon-button aria-label="Fermer" (click)="ref.close()"><app-icon [path]="icons.close" /></button>
    </div>
    <mat-dialog-content>
      @if (d(); as d) {
        <div class="title">
          <span class="sev" [attr.data-sev]="d.severity">{{ label(d.severity) }}</span>
          @if (d.statusCode) { <span class="status" [style.--c]="color(d.statusCode)">{{ d.statusCode }}</span> }
          <strong>{{ short(d.exceptionType || d.sourceContext || '') }}</strong>
        </div>
        <p class="msg">{{ d.message }}</p>
        <div class="trace">
          <code>{{ d.traceId }}</code>
          <button mat-icon-button aria-label="Copier le TraceId" (click)="copy(d.traceId)">
            <app-icon [path]="copied() ? icons.check : icons.copy" />
          </button>
          <button mat-button (click)="ref.close(d.traceId)"><app-icon [path]="icons.search" /> Tous les logs de ce TraceId</button>
        </div>
        <dl>
          <dt>Date (UTC)</dt><dd>{{ d.occurredAtUtc | date: 'dd/MM/yyyy HH:mm:ss' : 'UTC' }}</dd>
          <dt>Cprj</dt><dd>{{ d.cprj }}</dd>
          <dt>Application</dt><dd>{{ d.applicationName }}</dd>
          <dt>Requête</dt><dd>{{ d.httpMethod }} {{ d.path }}</dd>
          <dt>Utilisateur</dt><dd>{{ d.userName || '—' }} @if (d.userId) { <code>({{ d.userId }})</code> }</dd>
          <dt>Environnement</dt><dd>{{ d.environmentName || '—' }}</dd>
          <dt>Machine</dt><dd>{{ d.machineName || '—' }}</dd>
          <dt>Empreinte</dt><dd><code>{{ d.fingerprint || '—' }}</code></dd>
          <dt>Type / source</dt><dd><code>{{ d.exceptionType || d.sourceContext || '—' }}</code></dd>
        </dl>
        @if (d.claims) { <h4>Claims JWT</h4><pre>{{ d.claims }}</pre> }
        @if (d.exception) { <h4>Exception (stack trace)</h4><pre>{{ d.exception }}</pre> }
        @if (d.innerException) { <h4>Exception interne</h4><pre>{{ d.innerException }}</pre> }
      }
    </mat-dialog-content>
  `,
  styles: `
    .head { display: flex; align-items: center; justify-content: space-between; padding-right: .75rem; }
    .title { display: flex; align-items: center; gap: .6rem; font-size: 1.1rem; }
    .msg { margin: .75rem 0; }
    .trace { display: flex; align-items: center; flex-wrap: wrap; gap: .25rem; code { font-size: .8rem; } }
    dl { display: grid; grid-template-columns: 9rem 1fr; gap: .35rem .75rem; margin: 1rem 0;
      dt { color: var(--mat-sys-on-surface-variant); } dd { margin: 0; word-break: break-all; } }
    code, pre { font-family: ui-monospace, Consolas, monospace; }
    pre { margin: 0 0 1rem; padding: .9rem; border-radius: 12px; overflow: auto; max-height: 22rem; font-size: .78rem; line-height: 1.5;
      background: var(--mat-sys-surface-container); border: 1px solid var(--mat-sys-outline-variant); }
    h4 { margin: 1rem 0 .4rem; }
    .sev { padding: .1rem .55rem; border-radius: 999px; font-size: .75rem; font-weight: 700; color: #fff; }
    .sev[data-sev='Error'] { background: var(--sev-error); } .sev[data-sev='Warning'] { background: var(--sev-warning); color: #1f1300; } .sev[data-sev='Information'] { background: var(--sev-info); }
    .status { padding: .1rem .5rem; border-radius: 6px; font-weight: 700; font-size: .8rem; color: var(--c);
      background: color-mix(in srgb, var(--c) 16%, transparent); border: 1px solid color-mix(in srgb, var(--c) 45%, transparent); }
  `,
})
export class DetailDialog {
  protected readonly ref = inject<MatDialogRef<DetailDialog, string | undefined>>(MatDialogRef);
  private readonly row = inject<LogListItem>(MAT_DIALOG_DATA);
  private readonly api = inject(ApiService);
  protected readonly icons = ICONS;
  protected readonly d = signal<LogDetail | null>(this.row as LogDetail);
  protected readonly copied = signal(false);
  protected readonly label = severityLabel;
  protected readonly short = shortType;
  protected readonly color = statusColor;

  constructor() {
    this.api.log(this.row.id).subscribe({ next: x => this.d.set(x), error: () => undefined });
  }

  protected async copy(text: string): Promise<void> {
    try {
      await navigator.clipboard.writeText(text);
      this.copied.set(true);
      setTimeout(() => this.copied.set(false), 1500);
    } catch { /* presse-papiers indisponible */ }
  }
}
