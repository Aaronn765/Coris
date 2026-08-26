"use client";

import { useEffect, useMemo, useState } from "react";
import Link from "next/link";
import {
  ArrowUpRight,
  CalendarDays,
  ChevronDown,
  FolderKanban,
  Pencil,
  Plus,
  RotateCcw,
  Save,
  Search,
  Users,
  X,
} from "lucide-react";
import { apiService, PagedResult, ProjetQuery, ProjetWritePayload } from "@/services/api";
import { DomaineProjet, EtapeProjet, ProjetWithDetails, Responsable, StatutEtape, StatutProjet } from "@/types";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { formatDate } from "@/lib/formatters";

const clean = (value: string) => value.normalize("NFD").replace(/[\u0300-\u036f]/g, "").toLowerCase();
const progress = (value: number) => Math.max(0, Math.min(100, Math.round(value * 100)));

function statusClass(value: string) {
  const name = clean(value);
  if (name.includes("clotur")) return "coris-badge coris-badge-success";
  if (name.includes("suspend")) return "coris-badge coris-badge-danger";
  if (name.includes("en cours")) return "coris-badge coris-badge-warning";
  return "coris-badge coris-badge-info";
}

type StepDraft = {
  nom: string;
  tauxAvancement: string;
  statutEtapeId: number;
  dateDebut: string;
  dateFin: string;
  support: string;
  contraintes: string;
  commentaires: string;
};

const toDateInput = (value?: string) => value ? value.slice(0, 10) : "";

const toStepDraft = (step: EtapeProjet): StepDraft => ({
  nom: step.nom,
  tauxAvancement: String(progress(step.tauxAvancement)),
  statutEtapeId: step.statutEtapeId,
  dateDebut: toDateInput(step.dateDebut),
  dateFin: toDateInput(step.dateFin),
  support: step.support || "",
  contraintes: step.contraintes || "",
  commentaires: step.commentaires || "",
});

const newStepDraft = (statuses: StatutEtape[]): StepDraft => ({
  nom: "",
  tauxAvancement: "0",
  statutEtapeId: statuses[0]?.id || 1,
  dateDebut: "",
  dateFin: "",
  support: "",
  contraintes: "",
  commentaires: "",
});

const asOptional = (value: string) => value.trim() || undefined;

export default function ProjetsPage() {
  const [result, setResult] = useState<PagedResult<ProjetWithDetails>>({ items: [], page: 1, pageSize: 12, totalCount: 0, totalPages: 0 });
  const [query, setQuery] = useState<ProjetQuery>({ page: 1, pageSize: 12, sortBy: "dateEcheance", sortDirection: "asc" });
  const [searchDraft, setSearchDraft] = useState("");
  const [domains, setDomains] = useState<DomaineProjet[]>([]);
  const [statuses, setStatuses] = useState<StatutProjet[]>([]);
  const [stepStatuses, setStepStatuses] = useState<StatutEtape[]>([]);
  const [responsibles, setResponsibles] = useState<Responsable[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    Promise.all([
      apiService.getDomainesProjet(),
      apiService.getStatutsProjet(),
      apiService.getStatutsEtape(),
      apiService.getResponsables(),
    ])
      .then(([loadedDomains, loadedStatuses, loadedStepStatuses, loadedResponsibles]) => {
        setDomains(loadedDomains);
        setStatuses(loadedStatuses);
        setStepStatuses(loadedStepStatuses);
        setResponsibles(loadedResponsibles);
      })
      .catch((reason: Error) => setError(reason.message));
  }, []);

  useEffect(() => {
    const timer = window.setTimeout(() => {
      setLoading(true);
      setQuery((current) => ({ ...current, search: searchDraft.trim() || undefined, page: 1 }));
    }, 280);
    return () => window.clearTimeout(timer);
  }, [searchDraft]);

  useEffect(() => {
    let cancelled = false;
    apiService.getProjetsPage(query).then((data) => {
      if (!cancelled) { setResult(data); setError(null); }
    }).catch((reason: Error) => {
      if (!cancelled) setError(reason.message);
    }).finally(() => {
      if (!cancelled) setLoading(false);
    });
    return () => { cancelled = true; };
  }, [query]);

  const activeFilterCount = useMemo(() => [searchDraft, query.domaineProjetId, query.statutProjetId, query.responsableId].filter((value) => value !== undefined && value !== "").length, [query, searchDraft]);
  const updateQuery = (changes: Partial<ProjetQuery>) => {
    setLoading(true);
    setQuery((current) => ({ ...current, ...changes, page: changes.page ?? 1 }));
  };
  const clearFilters = () => { setSearchDraft(""); setQuery({ page: 1, pageSize: 12, sortBy: "dateEcheance", sortDirection: "asc" }); };
  const replaceProject = (updated: ProjetWithDetails) => {
    setResult((current) => ({ ...current, items: current.items.map((item) => item.id === updated.id ? updated : item) }));
  };

  return (
    <div className="animate-fade-up space-y-4 sm:space-y-6">
      <header className="flex flex-col justify-between gap-4 border-b border-slate-200/80 pb-4 sm:flex-row sm:items-end sm:pb-5"><div><h1 className="text-2xl font-bold tracking-tight text-slate-800 sm:text-3xl">Projets</h1><p className="mt-1 text-sm text-slate-500">Suivi des projets DSI et de leurs étapes</p></div><Link href="/projets/nouveau"><Button className="w-full rounded-xl bg-coris-red text-white shadow-lg shadow-red-900/15 hover:bg-[#c91428] sm:w-auto"><Plus className="mr-2 h-4 w-4" /> Nouveau projet</Button></Link></header>
      {error && <div role="alert" className="flex items-center justify-between gap-3 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700"><span>{error}</span><button type="button" aria-label="Fermer" onClick={() => setError(null)}><X className="h-4 w-4" /></button></div>}

      <section className="rounded-2xl border border-slate-200/80 bg-white p-4 shadow-[0_14px_45px_rgba(13,55,122,0.06)] sm:p-5"><div className="grid gap-3 lg:grid-cols-[minmax(0,1fr)_220px_190px_210px_auto]"><div className="relative"><Search className="pointer-events-none absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-coris-blue" /><input value={searchDraft} onChange={(event) => setSearchDraft(event.target.value)} placeholder="Rechercher un projet, domaine, étape..." className="h-11 w-full rounded-xl border border-slate-200 bg-white pl-10 pr-3 text-sm text-slate-700 outline-none focus:border-coris-blue focus:ring-4 focus:ring-coris-blue/10" /></div><select value={query.domaineProjetId ?? ""} onChange={(event) => updateQuery({ domaineProjetId: event.target.value ? Number(event.target.value) : undefined })} className="h-11 rounded-xl border border-slate-200 bg-white px-3 text-sm text-slate-700 outline-none focus:border-coris-blue focus:ring-4 focus:ring-coris-blue/10"><option value="">Tous les domaines</option>{domains.map((domain) => <option key={domain.id} value={domain.id}>{domain.nom}</option>)}</select><select value={query.statutProjetId ?? ""} onChange={(event) => updateQuery({ statutProjetId: event.target.value ? Number(event.target.value) : undefined })} className="h-11 rounded-xl border border-slate-200 bg-white px-3 text-sm text-slate-700 outline-none focus:border-coris-blue focus:ring-4 focus:ring-coris-blue/10"><option value="">Tous les statuts</option>{statuses.map((status) => <option key={status.id} value={status.id}>{status.nom}</option>)}</select><select value={query.responsableId ?? ""} onChange={(event) => updateQuery({ responsableId: event.target.value ? Number(event.target.value) : undefined })} className="h-11 rounded-xl border border-slate-200 bg-white px-3 text-sm text-slate-700 outline-none focus:border-coris-blue focus:ring-4 focus:ring-coris-blue/10"><option value="">Tous les responsables</option>{responsibles.map((responsible) => <option key={responsible.id} value={responsible.id}>{responsible.nom}</option>)}</select>{activeFilterCount > 0 && <Button type="button" variant="ghost" onClick={clearFilters} className="h-11 rounded-xl px-3 text-coris-red hover:bg-red-50 hover:text-coris-red"><RotateCcw className="mr-2 h-3.5 w-3.5" /> Réinitialiser</Button>}</div><div className="mt-3 flex items-center gap-2 text-xs font-semibold text-slate-400"><FolderKanban className="h-4 w-4 text-coris-blue" /> {result.totalCount} projet{result.totalCount > 1 ? "s" : ""} <span className="text-slate-300">·</span> {activeFilterCount} filtre{activeFilterCount > 1 ? "s" : ""} actif{activeFilterCount > 1 ? "s" : ""}</div></section>

      <section className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">{loading && result.items.length === 0 ? [1, 2, 3, 4, 5, 6].map((item) => <div key={item} className="h-64 animate-pulse rounded-2xl bg-white shadow-sm" />) : result.items.map((project) => <ProjectCard key={project.id} project={project} stepStatuses={stepStatuses} onSaved={replaceProject} />)}{!loading && result.items.length === 0 && <div className="col-span-full rounded-2xl border border-dashed border-slate-300 bg-white px-6 py-16 text-center text-sm text-slate-400">Aucun projet trouvé. Ajustez les filtres ou créez un nouveau projet.</div>}</section>

      <div className="flex flex-col items-center justify-between gap-3 px-1 py-2 text-xs text-slate-500 sm:flex-row"><span>Page <strong className="text-slate-700">{result.page}</strong> sur <strong className="text-slate-700">{Math.max(result.totalPages, 1)}</strong></span><div className="flex gap-2"><Button variant="outline" className="h-9 rounded-xl" disabled={result.page <= 1 || loading} onClick={() => updateQuery({ page: result.page - 1 })}>Précédent</Button><Button variant="outline" className="h-9 rounded-xl" disabled={result.page >= result.totalPages || loading} onClick={() => updateQuery({ page: result.page + 1 })}>Suivant</Button></div></div>
    </div>
  );
}

function ProjectCard({ project, stepStatuses, onSaved }: { project: ProjetWithDetails; stepStatuses: StatutEtape[]; onSaved: (project: ProjetWithDetails) => void }) {
  const percent = progress(project.tauxAvancement);
  const [editingSteps, setEditingSteps] = useState(false);
  const [saving, setSaving] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);
  const [stepDrafts, setStepDrafts] = useState<StepDraft[]>(() => project.etapes.map(toStepDraft));

  const updateStep = (index: number, changes: Partial<StepDraft>) => {
    setStepDrafts((current) => current.map((step, stepIndex) => stepIndex === index ? { ...step, ...changes } : step));
  };

  const saveSteps = async () => {
    if (stepDrafts.some((step) => !step.nom.trim())) {
      setSaveError("Chaque étape doit avoir un libellé.");
      return;
    }

    setSaving(true);
    setSaveError(null);
    try {
      const average = stepDrafts.length
        ? stepDrafts.reduce((sum, step) => sum + Math.max(0, Math.min(100, Number(step.tauxAvancement) || 0)), 0) / stepDrafts.length / 100
        : project.tauxAvancement;
      const payload: ProjetWritePayload = {
        domaineProjetId: project.domaineProjetId,
        nom: project.nom,
        description: project.description,
        tauxAvancement: average,
        dateDebut: project.dateDebut,
        dateFin: project.dateFin,
        dateEcheance: project.dateEcheance,
        statutProjetId: project.statutProjetId,
        support: project.support,
        acteursMetiers: project.acteursMetiers,
        contraintes: project.contraintes,
        commentaires: project.commentaires,
        responsableIds: project.responsables.map((responsible) => responsible.id),
        etapes: stepDrafts.map((step, index) => ({
          nom: step.nom.trim(),
          tauxAvancement: Math.max(0, Math.min(100, Number(step.tauxAvancement) || 0)) / 100,
          statutEtapeId: step.statutEtapeId,
          dateDebut: asOptional(step.dateDebut),
          dateFin: asOptional(step.dateFin),
          support: asOptional(step.support),
          contraintes: asOptional(step.contraintes),
          commentaires: asOptional(step.commentaires),
          ordre: index,
        })),
      };
      const updated = await apiService.updateProjet(project.id, payload);
      if (!updated) throw new Error("Le projet n'existe plus.");
      onSaved(updated);
      setEditingSteps(false);
    } catch (reason) {
      setSaveError(reason instanceof Error ? reason.message : "La mise à jour des étapes a échoué.");
    } finally {
      setSaving(false);
    }
  };

  return <article className="flex min-w-0 flex-col rounded-2xl border border-slate-200/80 bg-white p-5 shadow-[0_12px_35px_rgba(13,55,122,0.05)] transition-all hover:border-coris-blue/30 hover:shadow-[0_18px_45px_rgba(13,55,122,0.11)]"><div className="flex items-start justify-between gap-3"><Link href={`/projets/${project.id}`} className="flex min-w-0 items-center gap-3"><div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-coris-blue-soft text-coris-blue"><FolderKanban className="h-5 w-5" /></div><div className="min-w-0"><p className="font-mono text-[10px] font-bold tracking-[0.12em] text-slate-400">{project.numero}</p><h2 className="mt-1 line-clamp-2 text-base font-bold leading-5 text-slate-800 hover:text-coris-blue">{project.nom}</h2></div></Link><Link href={`/projets/${project.id}`} aria-label={`Ouvrir ${project.nom}`} className="shrink-0 text-slate-300 hover:text-coris-red"><ArrowUpRight className="h-4 w-4" /></Link></div><div className="mt-4 flex flex-wrap items-center gap-2"><span className="rounded-full bg-slate-100 px-2.5 py-1 text-[11px] font-semibold text-slate-600">{project.domaineProjet?.nom}</span><span className={statusClass(project.statutProjet?.nom || "")}>{project.statutProjet?.nom || "—"}</span></div><div className="mt-5"><div className="mb-2 flex items-center justify-between text-xs font-bold"><span className="text-slate-400">Avancement global</span><span className="text-coris-blue">{percent}%</span></div><div className="h-2 overflow-hidden rounded-full bg-slate-100"><div className="h-full rounded-full bg-coris-blue transition-all" style={{ width: `${percent}%` }} /></div></div><div className="mt-5 grid grid-cols-2 gap-3 border-t border-slate-100 pt-4 text-xs"><div className="flex min-w-0 items-center gap-2 text-slate-500"><CalendarDays className="h-3.5 w-3.5 shrink-0 text-coris-blue" /><span className="truncate">Échéance : {formatDate(project.dateEcheance)}</span></div><div className="flex min-w-0 items-center gap-2 text-slate-500"><Users className="h-3.5 w-3.5 shrink-0 text-coris-blue" /><span className="truncate">{project.responsables.length} responsable{project.responsables.length > 1 ? "s" : ""}</span></div></div>

    <div className="mt-4 border-t border-slate-100 pt-3">
      <details open className="group/steps">
        <summary className="flex cursor-pointer list-none items-center justify-between gap-3 text-xs font-bold text-slate-500 [&::-webkit-details-marker]:hidden"><span>{project.etapes.length} étape{project.etapes.length > 1 ? "s" : ""} — libellés et avancement</span><ChevronDown className="h-4 w-4 shrink-0 text-coris-blue transition-transform group-open/steps:rotate-180" /></summary>
        <div className="mt-3 space-y-2.5">
          {project.etapes.length === 0 && <p className="rounded-lg bg-slate-50 px-3 py-2 text-xs text-slate-400">Aucune étape enregistrée.</p>}
          {!editingSteps && project.etapes.map((step, index) => {
            const stepPercent = progress(step.tauxAvancement);
            return <div key={step.id} className="rounded-xl bg-slate-50/80 px-3 py-2.5"><div className="flex items-start justify-between gap-3"><div className="min-w-0"><p className="text-xs font-semibold leading-4 text-slate-700"><span className="mr-1 text-slate-400">{index + 1}.</span>{step.nom}</p><p className="mt-1 text-[10px] text-slate-400">{step.statutEtape?.nom || "—"}</p></div><span className="shrink-0 text-xs font-bold text-coris-blue">{stepPercent}%</span></div><div className="mt-2 h-1.5 overflow-hidden rounded-full bg-slate-200"><div className="h-full rounded-full bg-coris-blue" style={{ width: `${stepPercent}%` }} /></div></div>;
          })}
          {editingSteps && stepDrafts.map((step, index) => <div key={`${project.id}-step-${index}`} className="rounded-xl border border-coris-blue/15 bg-coris-blue-soft/20 p-3"><div className="grid gap-2 sm:grid-cols-[minmax(0,1fr)_82px]"><div className="space-y-1"><Label className="text-[10px]">Libellé</Label><Input value={step.nom} onChange={(event) => updateStep(index, { nom: event.target.value })} className="h-9 text-xs" /></div><div className="space-y-1"><Label className="text-[10px]">Taux (%)</Label><Input type="number" min="0" max="100" value={step.tauxAvancement} onChange={(event) => updateStep(index, { tauxAvancement: event.target.value })} className="h-9 text-xs" /></div></div><div className="mt-2 grid gap-2 sm:grid-cols-3"><div className="space-y-1 sm:col-span-1"><Label className="text-[10px]">Statut</Label><select value={step.statutEtapeId || ""} onChange={(event) => updateStep(index, { statutEtapeId: Number(event.target.value) })} className="h-9 w-full rounded-md border border-slate-200 bg-white px-2 text-xs text-slate-700 outline-none focus:border-coris-blue focus:ring-4 focus:ring-coris-blue/10">{stepStatuses.map((status) => <option key={status.id} value={status.id}>{status.nom}</option>)}</select></div><div className="space-y-1"><Label className="text-[10px]">Début</Label><Input type="date" value={step.dateDebut} onChange={(event) => updateStep(index, { dateDebut: event.target.value })} className="h-9 text-xs" /></div><div className="space-y-1"><Label className="text-[10px]">Fin</Label><Input type="date" value={step.dateFin} onChange={(event) => updateStep(index, { dateFin: event.target.value })} className="h-9 text-xs" /></div></div><details className="mt-2"><summary className="cursor-pointer text-[10px] font-bold text-coris-blue">Informations complémentaires</summary><div className="mt-2 grid gap-2"><Input value={step.support} onChange={(event) => updateStep(index, { support: event.target.value })} placeholder="Support" className="h-9 text-xs" /><Input value={step.contraintes} onChange={(event) => updateStep(index, { contraintes: event.target.value })} placeholder="Contraintes" className="h-9 text-xs" /><Textarea value={step.commentaires} onChange={(event) => updateStep(index, { commentaires: event.target.value })} placeholder="Commentaires" className="min-h-16 text-xs" /></div></details></div>)}
        </div>
      </details>
      {editingSteps && <Button type="button" variant="outline" onClick={() => setStepDrafts((current) => [...current, newStepDraft(stepStatuses)])} className="mt-3 h-9 w-full rounded-xl border-dashed border-coris-blue/30 text-xs text-coris-blue hover:bg-coris-blue-soft"><Plus className="mr-2 h-3.5 w-3.5" /> Ajouter une étape</Button>}
      {!editingSteps && project.etapes.length > 0 && <Button type="button" variant="outline" onClick={() => { setStepDrafts(project.etapes.map(toStepDraft)); setSaveError(null); setEditingSteps(true); }} className="mt-3 h-9 w-full rounded-xl border-coris-blue/20 text-xs text-coris-blue hover:bg-coris-blue-soft"><Pencil className="mr-2 h-3.5 w-3.5" /> Modifier les étapes</Button>}
      {editingSteps && <div className="mt-3 space-y-2"><div className="flex flex-col gap-2 sm:flex-row sm:justify-end"><Button type="button" variant="outline" onClick={() => { setEditingSteps(false); setSaveError(null); }} disabled={saving} className="h-9 rounded-xl text-xs">Annuler</Button><Button type="button" onClick={saveSteps} disabled={saving} className="h-9 rounded-xl bg-coris-blue text-xs text-white hover:bg-coris-blue/90"><Save className="mr-2 h-3.5 w-3.5" />{saving ? "Enregistrement..." : "Enregistrer les étapes"}</Button></div>{saveError && <p role="alert" className="text-xs text-red-600">{saveError}</p>}</div>}
    </div>
  </article>;
}
