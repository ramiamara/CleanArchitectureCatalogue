import { Injectable, computed, inject } from '@angular/core';
import { Coaching } from '../models/accompagnement.model';
import { AtelierScope, Entite, KpiFiltres, Perimetre } from '../models/kpi.model';
import { AxeDetail } from '../models/organisation.model';
import { AuthService } from './auth.service';
import { KpiDataService } from './kpi-data.service';

/**
 * Ce que chaque profil a le droit de voir :
 *  - CONSEILLER : ses propres accompagnements
 *  - DA         : les conseillers de son agence                 → détail par conseiller
 *  - DS         : les conseillers des agences de son secteur    → détail par agence
 *  - AN         : les conseillers qu'il a animés                → détail par conseiller
 *  - RA         : les accompagnements de ses animateurs         → détail par animateur
 */
@Injectable({ providedIn: 'root' })
export class PerimetreService {
  private readonly auth = inject(AuthService);
  private readonly data = inject(KpiDataService);

  /** Libellé du périmètre affiché dans l'en-tête ("Agence CONFLANS - HERBLAY", ...). */
  readonly libelle = computed(() => {
    const u = this.auth.utilisateur();
    if (!u) return '';
    switch (u.profil) {
      case 'CONSEILLER': {
        const c = this.data.collaborateur(u.perimetreId);
        return c ? `Agence ${this.data.agence(c.agenceId)?.libelle ?? ''}` : '';
      }
      case 'DA':
        return `Agence ${this.data.agence(u.perimetreId)?.libelle ?? ''}`;
      case 'DS':
        return `Secteur ${this.data.secteur(u.perimetreId)?.libelle ?? ''}`;
      case 'AN':
        return 'Conseillers animés';
      case 'RA':
        return 'Animateurs de mon équipe';
    }
  });

  /** Valeurs proposées dans le filtre "entité" (agences, conseillers ou animateurs visibles). */
  readonly entitesFiltrables = computed<Entite[]>(() => {
    const entites = new Map<string, Entite>();
    const ajouter = (collaborateurId: string, animateurId: string) => {
      const e = this.entiteDe(collaborateurId, animateurId);
      if (e && this.estVisible(collaborateurId, animateurId)) entites.set(e.id, e);
    };
    this.data.coachings().forEach((c) => ajouter(c.collaborateurId, c.animateurId));
    this.data.ateliers().forEach((a) => a.participants.forEach((p) => ajouter(p.collaborateurId, a.animateurId)));
    return [...entites.values()].sort((a, b) => a.libelle.localeCompare(b.libelle));
  });

  /** Construit le périmètre de calcul à partir des droits de l'utilisateur et des filtres. */
  construire(filtres: KpiFiltres): Perimetre {
    const garder = (collaborateurId: string, animateurId: string) =>
      this.estVisible(collaborateurId, animateurId) && this.respecteFiltres(collaborateurId, animateurId, filtres);

    const coachings =
      filtres.type === 'ATELIER'
        ? []
        : this.data
            .coachings()
            .filter((c) => this.respecteTypologieEtDuree(c.typologie, c.duree, filtres) && garder(c.collaborateurId, c.animateurId));

    const ateliers: AtelierScope[] =
      filtres.type === 'COACHING'
        ? []
        : this.data
            .ateliers()
            .filter((a) => this.respecteTypologieEtDuree(a.typologie, a.duree, filtres))
            .map((atelier) => ({ atelier, participants: atelier.participants.filter((p) => garder(p.collaborateurId, atelier.animateurId)) }))
            .filter((a) => a.participants.length > 0);

    return {
      profil: this.auth.profil(),
      filtres,
      coachings,
      ateliers,
      entiteDe: (collaborateurId, animateurId) => this.entiteDe(collaborateurId, animateurId),
    };
  }

  /** L'utilisateur connecté a-t-il le droit de voir ce coaching ? */
  peutVoir(c: Coaching): boolean {
    return this.estVisible(c.collaborateurId, c.animateurId);
  }

  // -------------------------------------------------------------------------
  private estVisible(collaborateurId: string, animateurId: string): boolean {
    const u = this.auth.utilisateur();
    if (!u) return false;
    const agenceId = this.data.collaborateur(collaborateurId)?.agenceId ?? '';
    switch (u.profil) {
      case 'CONSEILLER':
        return collaborateurId === u.perimetreId;
      case 'DA':
        return agenceId === u.perimetreId;
      case 'DS':
        return this.data.agence(agenceId)?.secteurId === u.perimetreId;
      case 'AN':
        return animateurId === u.perimetreId;
      case 'RA':
        return this.data.animateur(animateurId)?.responsableId === u.perimetreId;
    }
  }

  private respecteFiltres(collaborateurId: string, animateurId: string, f: KpiFiltres): boolean {
    const collab = this.data.collaborateur(collaborateurId);
    if (f.metier !== 'TOUS' && collab?.metier !== f.metier) return false;
    if (f.entiteId !== 'TOUS' && this.entiteDe(collaborateurId, animateurId)?.id !== f.entiteId) return false;
    return true;
  }

  private respecteTypologieEtDuree(typologie: string, duree: string, f: KpiFiltres): boolean {
    return (f.typologie === 'TOUS' || typologie === f.typologie) && (f.duree === 'TOUS' || duree === f.duree);
  }

  private entiteDe(collaborateurId: string, animateurId: string): Entite | null {
    const axe: AxeDetail | null = this.auth.config().axeDetail;
    const collab = this.data.collaborateur(collaborateurId);
    switch (axe) {
      case 'COLLABORATEUR':
        return collab
          ? { id: collab.id, libelle: `${collab.prenom} ${collab.nom}`, sousLibelle: this.data.agence(collab.agenceId)?.libelle }
          : null;
      case 'AGENCE': {
        const agence = collab ? this.data.agence(collab.agenceId) : undefined;
        return agence ? { id: agence.id, libelle: agence.libelle, sousLibelle: `DA : ${agence.directeur}` } : null;
      }
      case 'ANIMATEUR': {
        const an = this.data.animateur(animateurId);
        return an ? { id: an.id, libelle: `${an.prenom} ${an.nom}` } : null;
      }
      default:
        return null;
    }
  }
}
