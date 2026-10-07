import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

/** Redirige vers l'écran de connexion si aucun utilisateur n'est connecté. */
export const authGuard: CanActivateFn = () => {
  return inject(AuthService).estConnecte() || inject(Router).createUrlTree(['/connexion']);
};
