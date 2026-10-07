import { Injectable, computed, signal } from '@angular/core';
import { PROFILS, Utilisateur } from '../models/organisation.model';
import { UTILISATEURS } from '../mocks/organisation.mock';

const STORAGE_KEY = 'kpi.utilisateurId';

/**
 * Utilisateur connecté (simulé).
 * En réel : récupérer l'utilisateur et son profil depuis l'API / le jeton SSO.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  readonly comptesDemo = UTILISATEURS;

  private readonly utilisateurId = signal<string | null>(lireStorage());

  readonly utilisateur = computed<Utilisateur | undefined>(() => UTILISATEURS.find((u) => u.id === this.utilisateurId()));
  readonly estConnecte = computed(() => !!this.utilisateur());
  readonly profil = computed(() => this.utilisateur()?.profil ?? 'CONSEILLER');
  readonly config = computed(() => PROFILS[this.profil()]);

  connecter(utilisateurId: string): void {
    this.utilisateurId.set(utilisateurId);
    ecrireStorage(utilisateurId);
  }

  deconnecter(): void {
    this.utilisateurId.set(null);
    ecrireStorage(null);
  }
}

function lireStorage(): string | null {
  try {
    return sessionStorage.getItem(STORAGE_KEY);
  } catch {
    return null;
  }
}

function ecrireStorage(valeur: string | null): void {
  try {
    if (valeur) sessionStorage.setItem(STORAGE_KEY, valeur);
    else sessionStorage.removeItem(STORAGE_KEY);
  } catch {
    /* stockage indisponible : on garde l'état en mémoire */
  }
}
