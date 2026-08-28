import { ProjetWritePayload } from "@/services/api";
import { EtapeProjet, ProjetWithDetails, StatutEtape } from "@/types";

export type StepDraft = {
  nom: string;
  tauxAvancement: string;
  statutEtapeId: number;
  dateDebut: string;
  dateFin: string;
  support: string;
  contraintes: string;
  commentaires: string;
};

export const clean = (value: string) => value.normalize("NFD").replace(/[\u0300-\u036f]/g, "").toLowerCase();

export const progress = (value: number) => Math.max(0, Math.min(100, Math.round(value * 100)));

const toDateInput = (value?: string) => (value ? value.slice(0, 10) : "");

const asOptional = (value: string) => value.trim() || undefined;

const isoDate = (value: string) => (value ? `${value}T00:00:00.000Z` : undefined);

export const toStepDraft = (step: EtapeProjet): StepDraft => ({
  nom: step.nom,
  tauxAvancement: String(progress(step.tauxAvancement)),
  statutEtapeId: step.statutEtapeId,
  dateDebut: toDateInput(step.dateDebut),
  dateFin: toDateInput(step.dateFin),
  support: step.support || "",
  contraintes: step.contraintes || "",
  commentaires: step.commentaires || "",
});

export const newStepDraft = (statuses: StatutEtape[]): StepDraft => {
  const defaultStatus =
    statuses.find((status) => clean(status.nom).includes("non") || clean(status.nom).includes("plan"))?.id
    ?? statuses[0]?.id
    ?? 0;
  return {
    nom: "",
    tauxAvancement: "0",
    statutEtapeId: defaultStatus,
    dateDebut: "",
    dateFin: "",
    support: "",
    contraintes: "",
    commentaires: "",
  };
};

export function averageStepRate(steps: StepDraft[], fallback: number) {
  if (steps.length === 0) return fallback;
  const total = steps.reduce((sum, step) => sum + Math.max(0, Math.min(100, Number(step.tauxAvancement) || 0)), 0);
  return total / steps.length / 100;
}

export function buildProjetPayload(project: ProjetWithDetails, steps: StepDraft[]): ProjetWritePayload {
  return {
    domaineProjetId: project.domaineProjetId,
    nom: project.nom,
    description: project.description,
    tauxAvancement: averageStepRate(steps, project.tauxAvancement),
    dateDebut: project.dateDebut,
    dateFin: project.dateFin,
    dateEcheance: project.dateEcheance,
    statutProjetId: project.statutProjetId,
    support: project.support,
    acteursMetiers: project.acteursMetiers,
    contraintes: project.contraintes,
    commentaires: project.commentaires,
    responsableIds: project.responsables.map((responsible) => responsible.id),
    etapes: steps.map((step, index) => ({
      nom: step.nom.trim(),
      tauxAvancement: Math.max(0, Math.min(100, Number(step.tauxAvancement) || 0)) / 100,
      statutEtapeId: step.statutEtapeId,
      dateDebut: isoDate(step.dateDebut),
      dateFin: isoDate(step.dateFin),
      support: asOptional(step.support),
      contraintes: asOptional(step.contraintes),
      commentaires: asOptional(step.commentaires),
      ordre: index,
    })),
  };
}

const saveTimesKey = (projectId: number) => `projet-step-saves-${projectId}`;

export function readStepSaveTimes(projectId: number): Record<number, string> {
  if (typeof window === "undefined") return {};
  try {
    const raw = window.localStorage.getItem(saveTimesKey(projectId));
    return raw ? (JSON.parse(raw) as Record<number, string>) : {};
  } catch {
    return {};
  }
}

export function writeStepSaveTime(projectId: number, order: number, timestamp: string) {
  if (typeof window === "undefined") return;
  const current = readStepSaveTimes(projectId);
  current[order] = timestamp;
  window.localStorage.setItem(saveTimesKey(projectId), JSON.stringify(current));
}

export function statusClass(value: string) {
  const name = clean(value);
  if (name.includes("clotur") || name.includes("termin")) return "coris-badge coris-badge-success";
  if (name.includes("suspend")) return "coris-badge coris-badge-danger";
  if (name.includes("en cours") || name.includes("demarr")) return "coris-badge coris-badge-warning";
  return "coris-badge coris-badge-info";
}

export function stepAccentClass(value: string) {
  const name = clean(value);
  if (name.includes("clotur") || name.includes("termin")) return "border-l-emerald-500";
  if (name.includes("suspend")) return "border-l-red-400";
  if (name.includes("en cours") || name.includes("demarr")) return "border-l-amber-500";
  return "border-l-slate-300";
}
