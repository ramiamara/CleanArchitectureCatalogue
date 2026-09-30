import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { MatPaginatorIntl } from '@angular/material/paginator';
import { FrenchPaginatorIntl } from './paginator-fr';

export const appConfig: ApplicationConfig = {
  providers: [provideBrowserGlobalErrorListeners(), provideHttpClient(), { provide: MatPaginatorIntl, useClass: FrenchPaginatorIntl }],
};
