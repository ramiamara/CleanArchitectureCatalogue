import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: 'connexion',
    loadComponent: () => import('./features/connexion/connexion.component').then((m) => m.ConnexionComponent),
  },
  {
    // KPI-xx-1 : tableau de bord (le contenu dépend du profil connecté)
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./features/dashboard/dashboard.component').then((m) => m.DashboardComponent),
  },
  {
    // KPI-xx-02 : "Zoom vers inducteurs"
    path: 'inducteurs',
    canActivate: [authGuard],
    loadComponent: () => import('./features/inducteurs/inducteurs.component').then((m) => m.InducteursComponent),
  },
  {
    // KPI-xx-03 : matrice d'accompagnement d'un coaching
    path: 'coaching/:id',
    canActivate: [authGuard],
    loadComponent: () => import('./features/coaching-detail/coaching-detail.component').then((m) => m.CoachingDetailComponent),
  },
  { path: '**', redirectTo: '' },
];
