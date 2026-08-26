export interface BaseEntity {
  id: number;
  nom: string;
  actif: boolean;
  createdAt?: string;
  updatedAt?: string;
}

export type Responsable = BaseEntity;
export type Application = BaseEntity;
export type TypeIncident = BaseEntity;
export type Entite = BaseEntity;
export type Criticite = BaseEntity;
export type Risque = BaseEntity;
export type Statut = BaseEntity;
export type DomaineProjet = BaseEntity;
export type StatutProjet = BaseEntity;
export type StatutEtape = BaseEntity;

export interface Incident {
  id: number;
  numero: string;
  dateDeclaration: string; // ISO string
  dateFin?: string; // ISO string
  dureeMinutes?: number | null;
  dureeJours?: number | null;
  typeIncidentId: number;
  applicationId: number;
  intitule: string;
  description: string;
  entiteId: number;
  criticiteId: number;
  impact?: string;
  cause?: string;
  risqueId: number;
  actionsMenees?: string;
  solution?: string;
  actionsEnCours?: string;
  responsableN1Id: number;
  responsableN2Id?: number;
  mesuresPreventives?: string;
  statutId: number;
  createdAt?: string;
  updatedAt?: string;
}

export interface IncidentWithDetails extends Omit<Incident, 'typeIncidentId' | 'applicationId' | 'entiteId' | 'criticiteId' | 'risqueId' | 'statutId' | 'responsableN1Id' | 'responsableN2Id'> {
  typeIncident: TypeIncident;
  application: Application;
  entite: Entite;
  criticite: Criticite;
  risque: Risque;
  statut: Statut;
  responsableN1: Responsable;
  responsableN2?: Responsable;
  typeIncidentId: number;
  applicationId: number;
  entiteId: number;
  criticiteId: number;
  risqueId: number;
  statutId: number;
  responsableN1Id: number;
  responsableN2Id?: number;
}

export interface EtapeProjet {
  id: number;
  nom: string;
  tauxAvancement: number;
  statutEtapeId: number;
  statutEtape: StatutEtape;
  dateDebut?: string;
  dateFin?: string;
  support?: string;
  contraintes?: string;
  commentaires?: string;
  ordre: number;
}

export interface Projet {
  id: number;
  numero: string;
  domaineProjetId: number;
  nom: string;
  description?: string;
  tauxAvancement: number;
  dateDebut?: string;
  dateFin?: string;
  dateEcheance?: string;
  statutProjetId: number;
  support?: string;
  acteursMetiers?: string;
  contraintes?: string;
  commentaires?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface ProjetWithDetails extends Projet {
  domaineProjet: DomaineProjet;
  statutProjet: StatutProjet;
  responsables: Responsable[];
  etapes: EtapeProjet[];
}
