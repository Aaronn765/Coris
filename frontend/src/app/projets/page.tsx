"use client";

import { useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { ArrowUpRight, CalendarDays, FolderKanban, Plus, RotateCcw, Search, Users, X } from "lucide-react";
import { apiService, PagedResult, ProjetQuery } from "@/services/api";
import { DomaineProjet, ProjetWithDetails, Responsable, StatutProjet } from "@/types";
import { Button } from "@/components/ui/button";
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

export default function ProjetsPage() {
  const [result, setResult] = useState<PagedResult<ProjetWithDetails>>({ items: [], page: 1, pageSize: 12, totalCount: 0, totalPages: 0 });
  const [query, setQuery] = useState<ProjetQuery>({ page: 1, pageSize: 12, sortBy: "dateEcheance", sortDirection: "asc" });
  const [searchDraft, setSearchDraft] = useState("");
  const [domains, setDomains] = useState<DomaineProjet[]>([]);
  const [statuses, setStatuses] = useState<StatutProjet[]>([]);
  const [responsibles, setResponsibles] = useState<Responsable[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    Promise.all([apiService.getDomainesProjet(), apiService.getStatutsProjet(), apiService.getResponsables()])
      .then(([loadedDomains, loadedStatuses, loadedResponsibles]) => {
        setDomains(loadedDomains);
        setStatuses(loadedStatuses);
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

  return (
    <div className="animate-fade-up space-y-4 sm:space-y-6">
      <header className="flex flex-col justify-between gap-4 border-b border-slate-200/80 pb-4 sm:flex-row sm:items-end sm:pb-5"><div><h1 className="text-2xl font-bold tracking-tight text-slate-800 sm:text-3xl">Projets</h1><p className="mt-1 text-sm text-slate-500">Suivi des projets DSI et de leurs étapes</p></div><Link href="/projets/nouveau"><Button className="w-full rounded-xl bg-coris-red text-white shadow-lg shadow-red-900/15 hover:bg-[#c91428] sm:w-auto"><Plus className="mr-2 h-4 w-4" /> Nouveau projet</Button></Link></header>
      {error && <div role="alert" className="flex items-center justify-between gap-3 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700"><span>{error}</span><button type="button" aria-label="Fermer" onClick={() => setError(null)}><X className="h-4 w-4" /></button></div>}

      <section className="rounded-2xl border border-slate-200/80 bg-white p-4 shadow-[0_14px_45px_rgba(13,55,122,0.06)] sm:p-5"><div className="grid gap-3 lg:grid-cols-[minmax(0,1fr)_220px_190px_210px_auto]"><div className="relative"><Search className="pointer-events-none absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-coris-blue" /><input value={searchDraft} onChange={(event) => setSearchDraft(event.target.value)} placeholder="Rechercher un projet, domaine, étape..." className="h-11 w-full rounded-xl border border-slate-200 bg-white pl-10 pr-3 text-sm text-slate-700 outline-none focus:border-coris-blue focus:ring-4 focus:ring-coris-blue/10" /></div><select value={query.domaineProjetId ?? ""} onChange={(event) => updateQuery({ domaineProjetId: event.target.value ? Number(event.target.value) : undefined })} className="h-11 rounded-xl border border-slate-200 bg-white px-3 text-sm text-slate-700 outline-none focus:border-coris-blue focus:ring-4 focus:ring-coris-blue/10"><option value="">Tous les domaines</option>{domains.map((domain) => <option key={domain.id} value={domain.id}>{domain.nom}</option>)}</select><select value={query.statutProjetId ?? ""} onChange={(event) => updateQuery({ statutProjetId: event.target.value ? Number(event.target.value) : undefined })} className="h-11 rounded-xl border border-slate-200 bg-white px-3 text-sm text-slate-700 outline-none focus:border-coris-blue focus:ring-4 focus:ring-coris-blue/10"><option value="">Tous les statuts</option>{statuses.map((status) => <option key={status.id} value={status.id}>{status.nom}</option>)}</select><select value={query.responsableId ?? ""} onChange={(event) => updateQuery({ responsableId: event.target.value ? Number(event.target.value) : undefined })} className="h-11 rounded-xl border border-slate-200 bg-white px-3 text-sm text-slate-700 outline-none focus:border-coris-blue focus:ring-4 focus:ring-coris-blue/10"><option value="">Tous les responsables</option>{responsibles.map((responsible) => <option key={responsible.id} value={responsible.id}>{responsible.nom}</option>)}</select>{activeFilterCount > 0 && <Button type="button" variant="ghost" onClick={clearFilters} className="h-11 rounded-xl px-3 text-coris-red hover:bg-red-50 hover:text-coris-red"><RotateCcw className="mr-2 h-3.5 w-3.5" /> Réinitialiser</Button>}</div><div className="mt-3 flex items-center gap-2 text-xs font-semibold text-slate-400"><FolderKanban className="h-4 w-4 text-coris-blue" /> {result.totalCount} projet{result.totalCount > 1 ? "s" : ""} <span className="text-slate-300">·</span> {activeFilterCount} filtre{activeFilterCount > 1 ? "s" : ""} actif{activeFilterCount > 1 ? "s" : ""}</div></section>

      <section className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">{loading && result.items.length === 0 ? [1, 2, 3, 4, 5, 6].map((item) => <div key={item} className="h-64 animate-pulse rounded-2xl bg-white shadow-sm" />) : result.items.map((project) => <ProjectCard key={project.id} project={project} />)}{!loading && result.items.length === 0 && <div className="col-span-full rounded-2xl border border-dashed border-slate-300 bg-white px-6 py-16 text-center text-sm text-slate-400">Aucun projet trouvé. Ajustez les filtres ou créez un nouveau projet.</div>}</section>

      <div className="flex flex-col items-center justify-between gap-3 px-1 py-2 text-xs text-slate-500 sm:flex-row"><span>Page <strong className="text-slate-700">{result.page}</strong> sur <strong className="text-slate-700">{Math.max(result.totalPages, 1)}</strong></span><div className="flex gap-2"><Button variant="outline" className="h-9 rounded-xl" disabled={result.page <= 1 || loading} onClick={() => updateQuery({ page: result.page - 1 })}>Précédent</Button><Button variant="outline" className="h-9 rounded-xl" disabled={result.page >= result.totalPages || loading} onClick={() => updateQuery({ page: result.page + 1 })}>Suivant</Button></div></div>
    </div>
  );
}

function ProjectCard({ project }: { project: ProjetWithDetails }) {
  const percent = progress(project.tauxAvancement);
  return <Link href={`/projets/${project.id}`} className="group flex min-w-0 flex-col rounded-2xl border border-slate-200/80 bg-white p-5 shadow-[0_12px_35px_rgba(13,55,122,0.05)] transition-all hover:-translate-y-0.5 hover:border-coris-blue/30 hover:shadow-[0_18px_45px_rgba(13,55,122,0.11)]"><div className="flex items-start justify-between gap-3"><div className="flex min-w-0 items-center gap-3"><div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-coris-blue-soft text-coris-blue"><FolderKanban className="h-5 w-5" /></div><div className="min-w-0"><p className="font-mono text-[10px] font-bold tracking-[0.12em] text-slate-400">{project.numero}</p><h2 className="mt-1 line-clamp-2 text-base font-bold leading-5 text-slate-800 group-hover:text-coris-blue">{project.nom}</h2></div></div><ArrowUpRight className="h-4 w-4 shrink-0 text-slate-300 transition-transform group-hover:-translate-y-0.5 group-hover:translate-x-0.5 group-hover:text-coris-red" /></div><div className="mt-4 flex flex-wrap items-center gap-2"><span className="rounded-full bg-slate-100 px-2.5 py-1 text-[11px] font-semibold text-slate-600">{project.domaineProjet?.nom}</span><span className={statusClass(project.statutProjet?.nom || "")}>{project.statutProjet?.nom || "—"}</span></div><div className="mt-5"><div className="mb-2 flex items-center justify-between text-xs font-bold"><span className="text-slate-400">Avancement</span><span className="text-coris-blue">{percent}%</span></div><div className="h-2 overflow-hidden rounded-full bg-slate-100"><div className="h-full rounded-full bg-coris-blue transition-all" style={{ width: `${percent}%` }} /></div></div><div className="mt-5 grid grid-cols-2 gap-3 border-t border-slate-100 pt-4 text-xs"><div className="flex min-w-0 items-center gap-2 text-slate-500"><CalendarDays className="h-3.5 w-3.5 shrink-0 text-coris-blue" /><span className="truncate">Échéance : {formatDate(project.dateEcheance)}</span></div><div className="flex min-w-0 items-center gap-2 text-slate-500"><Users className="h-3.5 w-3.5 shrink-0 text-coris-blue" /><span className="truncate">{project.responsables.length} responsable{project.responsables.length > 1 ? "s" : ""}</span></div></div><p className="mt-3 text-[11px] font-semibold text-slate-400">{project.etapes.length} étape{project.etapes.length > 1 ? "s" : ""}</p></Link>;
}
