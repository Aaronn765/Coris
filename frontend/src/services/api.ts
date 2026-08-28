import {
  Application,
  Criticite,
  Entite,
  Incident,
  IncidentWithDetails,
  Responsable,
  Risque,
  Statut,
  TypeIncident,
  DomaineProjet,
  StatutProjet,
  StatutEtape,
  ProjetWithDetails,
} from "../types";

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface IncidentQuery {
  search?: string;
  dateDebut?: string;
  dateFin?: string;
  statutId?: number;
  typeIncidentId?: number;
  applicationId?: number;
  entiteId?: number;
  criticiteId?: number;
  risqueId?: number;
  responsableId?: number;
  dureeMinMinutes?: number;
  dureeMaxMinutes?: number;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDirection?: "asc" | "desc";
}

export interface ProjetQuery {
  search?: string;
  domaineProjetId?: number;
  statutProjetId?: number;
  responsableId?: number;
  tauxMin?: number;
  tauxMax?: number;
  dateEcheanceApres?: string;
  dateEcheanceAvant?: string;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDirection?: "asc" | "desc";
}

export interface ProjetWritePayload {
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
  responsableIds: number[];
  etapes: Array<{
    nom: string;
    tauxAvancement: number;
    statutEtapeId: number;
    dateDebut?: string;
    dateFin?: string;
    support?: string;
    contraintes?: string;
    commentaires?: string;
    ordre: number;
  }>;
}

export interface DashboardStats {
  totalIncidents: number;
  incidentsEnCours: number;
  incidentsClotures: number;
  incidentsParCriticite: DashboardCount[];
  incidentsParApplication: DashboardCount[];
  incidentsParType: DashboardCount[];
  evolution: DashboardEvolution[];
}

export interface DashboardCount {
  id: number;
  nom: string;
  count: number;
}

export interface DashboardEvolution {
  annee: number;
  mois: number;
  count: number;
}

export interface ProjectDashboardStats {
  totalProjets: number;
  projetsEnCours: number;
  projetsPlanifies: number;
  projetsClotures: number;
  tauxMoyen: number;
  projetsParDomaine: DashboardCount[];
  projetsParStatut: DashboardCount[];
}

export interface ReferentielSet {
  statuts: Statut[];
  criticites: Criticite[];
  applications: Application[];
  typesIncident: TypeIncident[];
  entites: Entite[];
  risques: Risque[];
  responsables: Responsable[];
  domainesProjet: DomaineProjet[];
  statutsProjet: StatutProjet[];
  statutsEtape: StatutEtape[];
}

type Referentiel =
  | "statuts"
  | "criticites"
  | "applications"
  | "typesIncident"
  | "entites"
  | "risques"
  | "responsables"
  | "domainesProjet"
  | "statutsProjet"
  | "statutsEtape";

const API_URL = (process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000/api").replace(/\/$/, "");

const referentielPaths: Record<Referentiel, string> = {
  statuts: "statuts",
  criticites: "criticites",
  applications: "applications",
  typesIncident: "types-incidents",
  entites: "entites",
  risques: "risques",
  responsables: "responsables",
  domainesProjet: "domaines-projet",
  statutsProjet: "statuts-projet",
  statutsEtape: "statuts-etape",
};

const referentielCache = new Map<string, Promise<ReferentielSet>>();

function clearReferentielCache() {
  referentielCache.clear();
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${API_URL}${path}`, {
    ...init,
    headers: {
      ...(init?.body ? { "Content-Type": "application/json" } : {}),
      ...init?.headers,
    },
    cache: "no-store",
  });

  if (!response.ok) {
    const error = await response.json().catch(() => null) as { detail?: string; title?: string } | null;
    throw new Error(`${error?.detail || error?.title || "Erreur API"} (${response.status})`);
  }

  if (response.status === 204) return undefined as T;
  return response.json() as Promise<T>;
}

function toQueryString(params: object) {
  const searchParams = new URLSearchParams();
  Object.entries(params as Record<string, string | number | undefined>).forEach(([key, value]) => {
    if (value !== undefined && value !== "") searchParams.set(key, String(value));
  });
  const query = searchParams.toString();
  return query ? `?${query}` : "";
}

function getReferentiels(includeInactive = false): Promise<ReferentielSet> {
  const cacheKey = includeInactive ? "all" : "active";
  const cached = referentielCache.get(cacheKey);
  if (cached) return cached;

  const query = includeInactive ? "?includeInactive=true" : "";
  const promise = request<ReferentielSet>(`/referentiels${query}`).catch((error) => {
    referentielCache.delete(cacheKey);
    throw error;
  });
  referentielCache.set(cacheKey, promise);
  return promise;
}

export const apiService = {
  getReferentiels,

  getStatuts: async (includeInactive = false): Promise<Statut[]> => (await getReferentiels(includeInactive)).statuts,
  getCriticites: async (includeInactive = false): Promise<Criticite[]> => (await getReferentiels(includeInactive)).criticites,
  getApplications: async (includeInactive = false): Promise<Application[]> => (await getReferentiels(includeInactive)).applications,
  getTypesIncident: async (includeInactive = false): Promise<TypeIncident[]> => (await getReferentiels(includeInactive)).typesIncident,
  getEntites: async (includeInactive = false): Promise<Entite[]> => (await getReferentiels(includeInactive)).entites,
  getRisques: async (includeInactive = false): Promise<Risque[]> => (await getReferentiels(includeInactive)).risques,
  getResponsables: async (includeInactive = false): Promise<Responsable[]> => (await getReferentiels(includeInactive)).responsables,
  getDomainesProjet: async (includeInactive = false): Promise<DomaineProjet[]> => (await getReferentiels(includeInactive)).domainesProjet,
  getStatutsProjet: async (includeInactive = false): Promise<StatutProjet[]> => (await getReferentiels(includeInactive)).statutsProjet,
  getStatutsEtape: async (includeInactive = false): Promise<StatutEtape[]> => (await getReferentiels(includeInactive)).statutsEtape,

  updateReferentiel: async (type: Referentiel, id: number, data: { nom: string; actif: boolean }) => {
    const result = await request(`/referentiels/${referentielPaths[type]}/${id}`, { method: "PUT", body: JSON.stringify(data) });
    clearReferentielCache();
    return result;
  },

  createReferentiel: async (type: Referentiel, data: { nom: string; actif: boolean }) => {
    const result = await request(`/referentiels/${referentielPaths[type]}`, { method: "POST", body: JSON.stringify(data) });
    clearReferentielCache();
    return result;
  },

  getIncidentsPage: (params: IncidentQuery = {}): Promise<PagedResult<IncidentWithDetails>> =>
    request<PagedResult<IncidentWithDetails>>(`/incidents${toQueryString(params)}`),

  getIncidents: async (params: IncidentQuery = {}): Promise<IncidentWithDetails[]> =>
    (await apiService.getIncidentsPage({ page: 1, pageSize: 100, ...params })).items,

  getIncidentById: (id: number): Promise<IncidentWithDetails | null> =>
    request<IncidentWithDetails>(`/incidents/${id}`).catch((error: Error) => {
      if (error.message.includes("404")) return null;
      throw error;
    }),

  createIncident: (incident: Partial<Incident>): Promise<IncidentWithDetails> =>
    request<IncidentWithDetails>("/incidents", { method: "POST", body: JSON.stringify(incident) }),

  updateIncident: (id: number, data: Partial<Incident>): Promise<IncidentWithDetails | null> =>
    request<IncidentWithDetails>(`/incidents/${id}`, { method: "PUT", body: JSON.stringify(data) }),

  getDashboardStats: (dateDebut?: string, dateFin?: string): Promise<DashboardStats> =>
    request<DashboardStats>(`/dashboard${toQueryString({ dateDebut, dateFin })}`),

  exportIncidents: async (params: IncidentQuery = {}): Promise<Blob> => {
    const response = await fetch(`${API_URL}/incidents/export${toQueryString(params)}`, { cache: "no-store" });
    if (!response.ok) throw new Error(`Erreur d'export (${response.status})`);
    return response.blob();
  },

  getProjetsPage: (params: ProjetQuery = {}): Promise<PagedResult<ProjetWithDetails>> =>
    request<PagedResult<ProjetWithDetails>>(`/projets${toQueryString(params)}`),

  exportProjets: async (params: ProjetQuery = {}): Promise<Blob> => {
    const response = await fetch(`${API_URL}/projets/export${toQueryString(params)}`, { cache: "no-store" });
    if (!response.ok) throw new Error(`Erreur d'export (${response.status})`);
    return response.blob();
  },

  getProjets: async (params: ProjetQuery = {}): Promise<ProjetWithDetails[]> =>
    (await apiService.getProjetsPage({ page: 1, pageSize: 100, ...params })).items,

  getProjetById: (id: number): Promise<ProjetWithDetails | null> =>
    request<ProjetWithDetails>(`/projets/${id}`).catch((error: Error) => {
      if (error.message.includes("404")) return null;
      throw error;
    }),

  createProjet: (project: ProjetWritePayload): Promise<ProjetWithDetails> =>
    request<ProjetWithDetails>("/projets", { method: "POST", body: JSON.stringify(project) }),

  updateProjet: (id: number, project: ProjetWritePayload): Promise<ProjetWithDetails | null> =>
    request<ProjetWithDetails>(`/projets/${id}`, { method: "PUT", body: JSON.stringify(project) }),

  exportProjetPdf: async (id: number): Promise<Blob> => {
    const response = await fetch(`${API_URL}/projets/${id}/export`, { cache: "no-store" });
    if (!response.ok) throw new Error(`Erreur d'export PDF (${response.status})`);
    return response.blob();
  },

  getProjectDashboardStats: (): Promise<ProjectDashboardStats> =>
    request<ProjectDashboardStats>("/dashboard/projets"),
};
