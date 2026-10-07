import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { ApiService } from './api.service';
import { IconComponent } from './icon.component';
import { LogDetail, LogListItem } from './models';
import { severityLabel, shortType, statusColor } from './format';
import { TraceDialog } from './trace-dialog';

@Component({
  selector: 'app-detail-dialog',
  imports: [DatePipe, MatButtonModule, MatDialogModule, IconComponent],
  templateUrl: './detail-dialog.component.html',
  styleUrls: ['./detail-panel.scss', './detail-dialog.component.scss'],
})
export class DetailDialogComponent extends TraceDialog {
  private readonly row = inject<LogListItem>(MAT_DIALOG_DATA);
  private readonly api = inject(ApiService);
  protected readonly d = signal<LogDetail | null>(this.row as LogDetail);
  protected readonly label = severityLabel;
  protected readonly short = shortType;
  protected readonly color = statusColor;

  constructor() {
    super();
    this.api.log(this.row.id).subscribe({ next: x => this.d.set(x), error: () => undefined });
  }
}
