import { inject, signal } from '@angular/core';
import { MatDialogRef } from '@angular/material/dialog';
import { ICONS } from './icons';
import { TraceTarget } from './models';

/** Socle des panneaux de détail : fermeture, copie du TraceId et navigation vers les éléments d'un TraceId. */
export abstract class TraceDialog {
  protected readonly ref = inject<MatDialogRef<unknown, TraceTarget | undefined>>(MatDialogRef);
  protected readonly icons = ICONS;
  protected readonly copied = signal(false);

  protected go(traceId: string, view: TraceTarget['view']): void {
    this.ref.close({ traceId, view });
  }

  protected async copy(text: string): Promise<void> {
    try {
      await navigator.clipboard.writeText(text);
      this.copied.set(true);
      setTimeout(() => this.copied.set(false), 1500);
    } catch {
      // Presse-papiers indisponible : rien à faire.
    }
  }
}
