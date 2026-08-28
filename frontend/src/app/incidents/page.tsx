"use client";

import { useEffect, useMemo, useState } from "react";
import { SortingState } from "@tanstack/react-table";
import {
  CalendarDays,
  Download,
  ListFilter,
  Loader2,
  Plus,
  RotateCcw,
  Search,
  SlidersHorizontal,
  X,
} from "lucide-react";
import Link from "next/link";
import { apiService, IncidentQuery, PagedResult } from "@/services/api";
import { BaseEntity, IncidentWithDetails } from "@/types";
import { DataTable } from "@/components/incidents/data-table";
import { columns } from "./columns";
import { Button } from "@/components/ui/button";

const sortMap: Record<string, string> = {
  "application.nom": "application",
  "typeIncident.nom": "typeIncident",
  "criticite.nom": "criticite",
  "statut.nom": "statut",
};

const durationOptions = [
  { value: "", label: "Toutes les durées" },
  { value: "0-59", label: "Moins d'une heure" },
  { value: "60-239", label: "De 1 à 4 heures" },
  { value: "240-1439", label: "De 4 à 24 heures" },
  { value: "1440-10079", label: "De 1 à 7 jours" },
  { value: "10080-", label: "Plus de 7 jours" },
];

type ReferenceKey = "statuts" | "typesIncident" | "applications" | "entites" | "criticites" | "risques" | "responsables";

function FilterSelect({
  label,
  value,
  options,
  placeholder,
  onChange,
}: {
  label: string;
  value?: number;
  options: BaseEntity[];
  placeholder: string;
  onChange: (value?: number) => void;
}) {
  return (
    <label className="space-y-1.5">
      <span className="block text-[11px] font-bold uppercase tracking-[0.1em] text-slate-500">{label}</span>
      <select
        value={value ?? ""}
        onChange={(event) => onChange(event.target.value ? Number(event.target.value) : undefined)}
        className="h-11 w-full rounded-xl border border-slate-200 bg-white px-3 text-sm text-slate-700 outline-none transition-all focus:border-coris-blue focus:ring-4 focus:ring-coris-blue/10"
      >
        <option value="">{placeholder}</option>
        {options.map((option) => <option key={option.id} value={option.id}>{option.nom}</option>)}
      </select>
    </label>
  );
}

export default function IncidentsPage() {
  const [result, setResult] = useState<PagedResult<IncidentWithDetails>>({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 });
  const [query, setQuery] = useState<IncidentQuery>({ page: 1, pageSize: 20, sortBy: "dateDeclaration", sortDirection: "desc" });
  const [searchDraft, setSearchDraft] = useState("");
  const [sorting, setSorting] = useState<SortingState>([{ id: "dateDeclaration", desc: true }]);
  const [references, setReferences] = useState<Record<ReferenceKey, BaseEntity[]>>({
    statuts: [], typesIncident: [], applications: [], entites: [], criticites: [], risques: [], responsables: [],
  });
  const [loading, setLoading] = useState(true);
  const [exporting, setExporting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    Promise.all([
      apiService.getStatuts(), apiService.getTypesIncident(), apiService.getApplications(),
      apiService.getEntites(), apiService.getCriticites(), apiService.getRisques(), apiService.getResponsables(),
    ]).then(([statuts, typesIncident, applications, entites, criticites, risques, responsables]) => {
      if (!cancelled) setReferences({ statuts, typesIncident, applications, entites, criticites, risques, responsables });
    }).catch((reason: Error) => {
      if (!cancelled) setError(reason.message);
    });
    return () => { cancelled = true; };
  }, []);

  useEffect(() => {
    const timer = window.setTimeout(() => {
      setLoading(true);
      setQuery((current) => ({ ...current, search: searchDraft.trim() || undefined, page: 1 }));
    }, 320);
    return () => window.clearTimeout(timer);
  }, [searchDraft]);

  useEffect(() => {
    let cancelled = false;
    apiService.getIncidentsPage(query).then((data) => {
      if (!cancelled) {
        setResult(data);
        setError(null);
      }
    }).catch((reason: Error) => {
      if (!cancelled) setError(reason.message);
    }).finally(() => {
      if (!cancelled) setLoading(false);
    });
    return () => { cancelled = true; };
  }, [query]);

  const activeFilterCount = useMemo(() => [
    searchDraft, query.dateDebut, query.dateFin, query.statutId, query.typeIncidentId,
    query.applicationId, query.entiteId, query.criticiteId, query.risqueId,
    query.responsableId, query.dureeMinMinutes, query.dureeMaxMinutes,
  ].filter((value) => value !== undefined && value !== "").length, [query, searchDraft]);

  const updateQuery = (changes: Partial<IncidentQuery>) => {
    setLoading(true);
    setQuery((current) => ({ ...current, ...changes, page: changes.page ?? 1 }));
  };

  const clearFilters = () => {
    setLoading(true);
    setSearchDraft("");
    setQuery({ page: 1, pageSize: 20, sortBy: "dateDeclaration", sortDirection: "desc" });
    setSorting([{ id: "dateDeclaration", desc: true }]);
  };

  const setReferenceFilter = (key: keyof IncidentQuery, value?: number) => updateQuery({ [key]: value } as Partial<IncidentQuery>);

  const setDuration = (value: string) => {
    if (!value) return updateQuery({ dureeMinMinutes: undefined, dureeMaxMinutes: undefined });
    const [min, max] = value.split("-");
    updateQuery({ dureeMinMinutes: Number(min), dureeMaxMinutes: max ? Number(max) : undefined });
  };

  const selectedDuration = query.dureeMinMinutes === undefined
    ? ""
    : `${query.dureeMinMinutes}-${query.dureeMaxMinutes ?? ""}`;

  const handleSortingChange = (next: SortingState | ((current: SortingState) => SortingState)) => {
    const nextSorting = typeof next === "function" ? next(sorting) : next;
    setSorting(nextSorting);
    const first = nextSorting[0];
    updateQuery({ sortBy: first ? (sortMap[first.id] || first.id) : "dateDeclaration", sortDirection: first?.desc ? "desc" : "asc" });
  };

  const handleExport = async () => {
    setExporting(true);
    try {
      const blob = await apiService.exportIncidents(query);
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement("a");
      anchor.href = url;
      anchor.download = "incidents-export.xlsx";
      anchor.rel = "noopener";
      anchor.style.display = "none";
      document.body.appendChild(anchor);
      anchor.click();
      anchor.remove();
      window.setTimeout(() => URL.revokeObjectURL(url), 1000);
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "L'export a échoué.");
    } finally {
      setExporting(false);
    }
  };

  return (
    <div className="animate-fade-up space-y-4 sm:space-y-6">
      <header className="flex flex-col justify-between gap-4 border-b border-slate-200/80 pb-4 sm:flex-row sm:items-end sm:pb-5">
        <div><h1 className="text-2xl font-bold tracking-tight text-slate-800 sm:text-3xl">Incidents</h1></div>
        <div className="grid gap-2 sm:flex sm:flex-wrap">
          <Button onClick={handleExport} disabled={exporting} variant="outline" className="h-10 w-full rounded-xl border-slate-200 text-coris-blue hover:border-coris-blue hover:bg-coris-blue-soft sm:w-auto">
            {exporting ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : <Download className="mr-2 h-4 w-4" />}
            Exporter Excel
          </Button>
          <Link href="/incidents/nouveau" className="block"><Button className="h-10 w-full rounded-xl bg-coris-red text-white shadow-lg shadow-red-900/15 hover:bg-[#c91428] sm:w-auto"><Plus className="mr-2 h-4 w-4" /> Nouvel incident</Button></Link>
        </div>
      </header>

      {error && <div role="alert" className="flex items-center justify-between gap-3 rounded-2xl border border-red-200 bg-white px-4 py-3 text-sm text-red-700"><span>{error}</span><button type="button" aria-label="Fermer" onClick={() => setError(null)}><X className="h-4 w-4" /></button></div>}

      <section className="overflow-hidden rounded-xl border border-slate-200/80 bg-white shadow-[0_14px_45px_rgba(13,55,122,0.06)] sm:rounded-[1.5rem]">
        <div className="border-b border-slate-100 bg-white px-4 py-4 sm:px-6">
          <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
            <div className="flex items-center gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-coris-blue text-white shadow-lg shadow-blue-900/20"><SlidersHorizontal className="h-5 w-5" /></div>
              <div><h2 className="font-bold text-slate-800">Recherche & filtres</h2></div>
            </div>
            <div className="flex items-center gap-2 text-xs font-semibold text-coris-blue"><ListFilter className="h-4 w-4" /> {activeFilterCount} filtre{activeFilterCount > 1 ? "s" : ""} actif{activeFilterCount > 1 ? "s" : ""}</div>
          </div>
          <div className="relative mt-4 sm:mt-5">
            <Search className="pointer-events-none absolute left-4 top-1/2 h-4 w-4 -translate-y-1/2 text-coris-blue" />
            <input value={searchDraft} onChange={(event) => setSearchDraft(event.target.value)} placeholder="Rechercher un numéro, intitulé, application, responsable, cause ou solution…" className="h-12 w-full rounded-xl border border-blue-100 bg-white pl-11 pr-4 text-sm text-slate-700 shadow-sm outline-none transition-all placeholder:text-slate-400 focus:border-coris-blue focus:ring-4 focus:ring-coris-blue/10" />
          </div>
        </div>
        <div className="grid gap-4 px-4 py-4 sm:grid-cols-2 sm:px-6 sm:py-5 lg:grid-cols-3 xl:grid-cols-5">
          <label className="space-y-1.5"><span className="block text-[11px] font-bold uppercase tracking-[0.1em] text-slate-500">Du (déclaration)</span><span className="relative block"><CalendarDays className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-coris-blue" /><input type="date" value={query.dateDebut || ""} onChange={(event) => updateQuery({ dateDebut: event.target.value || undefined })} className="h-11 w-full rounded-xl border border-slate-200 bg-white pl-10 pr-3 text-sm text-slate-700 outline-none transition-all focus:border-coris-blue focus:ring-4 focus:ring-coris-blue/10" /></span></label>
          <label className="space-y-1.5"><span className="block text-[11px] font-bold uppercase tracking-[0.1em] text-slate-500">Au (déclaration)</span><span className="relative block"><CalendarDays className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-coris-blue" /><input type="date" value={query.dateFin || ""} onChange={(event) => updateQuery({ dateFin: event.target.value || undefined })} className="h-11 w-full rounded-xl border border-slate-200 bg-white pl-10 pr-3 text-sm text-slate-700 outline-none transition-all focus:border-coris-blue focus:ring-4 focus:ring-coris-blue/10" /></span></label>
          <label className="space-y-1.5"><span className="block text-[11px] font-bold uppercase tracking-[0.1em] text-slate-500">Durée de résolution</span><select value={selectedDuration} onChange={(event) => setDuration(event.target.value)} className="h-11 w-full rounded-xl border border-slate-200 bg-white px-3 text-sm text-slate-700 outline-none transition-all focus:border-coris-blue focus:ring-4 focus:ring-coris-blue/10">{durationOptions.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}</select></label>
          <FilterSelect label="Statut" value={query.statutId} options={references.statuts} placeholder="Tous les statuts" onChange={(value) => setReferenceFilter("statutId", value)} />
          <FilterSelect label="Type d'incident" value={query.typeIncidentId} options={references.typesIncident} placeholder="Tous les types" onChange={(value) => setReferenceFilter("typeIncidentId", value)} />
          <FilterSelect label="Application" value={query.applicationId} options={references.applications} placeholder="Toutes les applications" onChange={(value) => setReferenceFilter("applicationId", value)} />
          <FilterSelect label="Entité" value={query.entiteId} options={references.entites} placeholder="Toutes les entités" onChange={(value) => setReferenceFilter("entiteId", value)} />
          <FilterSelect label="Criticité" value={query.criticiteId} options={references.criticites} placeholder="Toutes les criticités" onChange={(value) => setReferenceFilter("criticiteId", value)} />
          <FilterSelect label="Risque" value={query.risqueId} options={references.risques} placeholder="Tous les risques" onChange={(value) => setReferenceFilter("risqueId", value)} />
          <FilterSelect label="Responsable" value={query.responsableId} options={references.responsables} placeholder="Tous les responsables" onChange={(value) => setReferenceFilter("responsableId", value)} />
        </div>
        <div className="flex flex-wrap items-center justify-end gap-3 border-t border-slate-100 px-4 py-3 sm:px-6">
          {activeFilterCount > 0 && <Button type="button" variant="ghost" onClick={clearFilters} className="h-9 rounded-xl px-3 text-coris-red hover:bg-white hover:text-coris-red"><RotateCcw className="mr-2 h-3.5 w-3.5" /> Réinitialiser</Button>}
        </div>
      </section>

      <section className="space-y-3">
        <DataTable columns={columns} data={result.items} loading={loading} serverState={{
          pageIndex: (result.page || 1) - 1,
          pageSize: result.pageSize,
          pageCount: result.totalPages,
          onPageChange: (pageIndex) => setQuery((current) => ({ ...current, page: pageIndex + 1 })),
          sorting,
          onSortingChange: handleSortingChange,
          globalFilter: searchDraft,
          onGlobalFilterChange: (next) => {
            const value = typeof next === "function" ? next(searchDraft) : next;
            setSearchDraft(value);
          },
        }} />
      </section>
    </div>
  );
}
