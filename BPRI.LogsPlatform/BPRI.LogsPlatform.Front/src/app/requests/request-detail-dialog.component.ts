import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { ApiService } from '../api.service';
import { durationClass, formatDuration, prettyBody, statusColor } from '../format';
import { IconComponent } from '../icon.component';
import { RequestDetail, RequestListItem } from '../models';
import { TraceDialog } from '../trace-dialog';

@Component({
  selector: 'app-request-detail-dialog',
  imports: [DatePipe, MatButtonModule, MatDialogModule, IconComponent],
  templateUrl: './request-detail-dialog.component.html',
  styleUrls: ['../detail-panel.scss', './request-detail-dialog.component.scss'],
})
export class RequestDetailDialogComponent extends TraceDialog {
  private readonly row = inject<RequestListItem>(MAT_DIALOG_DATA);
  private readonly api = inject(ApiService);
  protected readonly d = signal<RequestDetail | null>(this.row as RequestDetail);
  protected readonly color = statusColor;
  protected readonly format = formatDuration;
  protected readonly duration = durationClass;
  protected readonly pretty = prettyBody;

  constructor() {
    super();
    this.api.request(this.row.id).subscribe({ next: x => this.d.set(x), error: () => undefined });
  }
}
