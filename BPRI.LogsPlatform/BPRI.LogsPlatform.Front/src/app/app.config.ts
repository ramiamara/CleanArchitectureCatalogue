import { ApplicationConfig } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { MatPaginatorIntl } from '@angular/material/paginator';
import { FrenchPaginatorIntl } from './paginator-fr';

export const appConfig: ApplicationConfig = {
  providers: [provideHttpClient(), { provide: MatPaginatorIntl, useClass: FrenchPaginatorIntl }],
};
