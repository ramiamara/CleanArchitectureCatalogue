import { Component, input } from '@angular/core';

@Component({
  selector: 'app-icon',
  
  template: `<svg viewBox="0 0 24 24" width="20" height="20" fill="currentColor" aria-hidden="true"><path [attr.d]="path()" /></svg>`,
  styles: `:host { display: inline-flex; vertical-align: middle; } svg { display: block; }`,
})
export class Icon {
  readonly path = input.required<string>();
}
