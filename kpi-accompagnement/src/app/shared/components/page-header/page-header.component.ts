import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

/** En-tête de page : code page (KPI-DA-1...), titre, sous-titre et lien retour optionnel. */
@Component({
  selector: 'app-page-header',
  imports: [RouterLink],
  templateUrl: './page-header.component.html',
  styleUrl: './page-header.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PageHeaderComponent {
  readonly code = input.required<string>();
  readonly titre = input.required<string>();
  readonly sousTitre = input('');
  readonly retour = input(false);
}
