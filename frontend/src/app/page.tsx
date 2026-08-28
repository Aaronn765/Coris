"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import Link from "next/link";
import {
  AlertCircle,
  ArrowUpRight,
  BarChart3,
  CalendarCheck,
  CheckCircle2,
  Clock3,
  Download,
  FolderKanban,
  Layers3,
  ShieldAlert,
} from "lucide-react";
import { apiService, DashboardCount, DashboardStats, ProjectDashboardStats } from "@/services/api";
import { IncidentWithDetails, ProjetWithDetails } from "@/types";
import { formatDate, formatDuration } from "@/lib/formatters";

const emptyStats: DashboardStats = {
  totalIncidents: 0,
  incidentsEnCours: 0,
  incidentsClotures: 0,
  incidentsParCriticite: [],
  incidentsParApplication: [],
  incidentsParType: [],
  evolution: [],
};

const emptyProjectStats: ProjectDashboardStats = {
  totalProjets: 0,
  projetsEnCours: 0,
  projetsPlanifies: 0,
  projetsClotures: 0,
  tauxMoyen: 0,
  projetsParDomaine: [],
  projetsParStatut: [],
};

const monthLabels = ["Jan", "Fev", "Mar", "Avr", "Mai", "Juin", "Juil", "Aou", "Sep", "Oct", "Nov", "Dec"];
const analysisTabs = [
  { id: "monthly", label: "Par mois" },
  { id: "type", label: "Par type" },
  { id: "application", label: "Par applicatif" },
] as const;

type AnalysisTab = (typeof analysisTabs)[number]["id"];

const statusClass = (value: string) => {
  const clean = value.normalize("NFD").replace(/[\u0300-\u036f]/g, "").toLowerCase();
  return ["resolu", "cloture", "ferme"].includes(clean) ? "coris-badge coris-badge-success" : "coris-badge coris-badge-warning";
};

function yearRange(year: number) {
  return {
    dateDebut: `${year}-01-01`,
    dateFin: `${year}-12-31`,
  };
}

function exportSvg(svgElement: SVGSVGElement | null, filename: string) {
  if (!svgElement) return;

  const svg = svgElement.cloneNode(true) as SVGSVGElement;
  svg.setAttribute("xmlns", "http://www.w3.org/2000/svg");
  const content = new XMLSerializer().serializeToString(svg);
  const blob = new Blob([`<?xml version="1.0" encoding="UTF-8"?>\n${content}`], { type: "image/svg+xml;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement("a");
  anchor.href = url;
  anchor.download = filename;
  anchor.rel = "noopener";
  anchor.style.display = "none";
  document.body.appendChild(anchor);
  anchor.click();
  anchor.remove();
  window.setTimeout(() => URL.revokeObjectURL(url), 1000);
}

export default function Dashboard() {
  const [stats, setStats] = useState<DashboardStats>(emptyStats);
  const [analysisStats, setAnalysisStats] = useState<DashboardStats>(emptyStats);
  const [recentIncidents, setRecentIncidents] = useState<IncidentWithDetails[]>([]);
  const [projectStats, setProjectStats] = useState<ProjectDashboardStats>(emptyProjectStats);
  const [recentProjects, setRecentProjects] = useState<ProjetWithDetails[]>([]);
  const [dashboardTab, setDashboardTab] = useState<"incidents" | "projets">("incidents");
  const [selectedYear, setSelectedYear] = useState<number | null>(null);
  const [activeTab, setActiveTab] = useState<AnalysisTab>("monthly");
  const [loading, setLoading] = useState(true);
  const [analysisLoading, setAnalysisLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const chartRef = useRef<SVGSVGElement | null>(null);

  useEffect(() => {
    Promise.all([
      apiService.getDashboardStats(),
      apiService.getIncidentsPage({ page: 1, pageSize: 5, sortBy: "dateDeclaration", sortDirection: "desc" }),
      apiService.getProjectDashboardStats(),
      apiService.getProjetsPage({ page: 1, pageSize: 5, sortBy: "dateEcheance", sortDirection: "asc" }),
    ]).then(([dashboard, incidents, projectsDashboard, projects]) => {
      const years = Array.from(new Set(dashboard.evolution.map((item) => item.annee))).sort((a, b) => b - a);
      setStats(dashboard);
      setAnalysisStats(dashboard);
      setRecentIncidents(incidents.items);
      setProjectStats(projectsDashboard);
      setRecentProjects(projects.items);
      setSelectedYear((current) => current ?? years[0] ?? null);
    }).catch((reason: Error) => setError(reason.message)).finally(() => setLoading(false));
  }, []);

  useEffect(() => {
    if (selectedYear === null) return;

    const controller = new AbortController();
    const loadingTimer = window.setTimeout(() => setAnalysisLoading(true), 0);
    const { dateDebut, dateFin } = yearRange(selectedYear);
    apiService.getDashboardStats(dateDebut, dateFin)
      .then(setAnalysisStats)
      .catch((reason: Error) => {
        if (!controller.signal.aborted) setError(reason.message);
      })
      .finally(() => {
        if (!controller.signal.aborted) setAnalysisLoading(false);
      });

    return () => {
      controller.abort();
      window.clearTimeout(loadingTimer);
    };
  }, [selectedYear]);

  const availableYears = useMemo(
    () => Array.from(new Set(stats.evolution.map((item) => item.annee))).sort((a, b) => b - a),
    [stats.evolution],
  );

  const monthlyData = useMemo(() => {
    const values = monthLabels.map((label, index) => ({ label, month: index + 1, count: 0 }));
    if (selectedYear === null) return values;

    for (const item of analysisStats.evolution) {
      if (item.annee === selectedYear && item.mois >= 1 && item.mois <= 12) {
        values[item.mois - 1].count = item.count;
      }
    }

    return values;
  }, [analysisStats.evolution, selectedYear]);

  const maxMonthlyCount = Math.max(...monthlyData.map((item) => item.count), 1);
  const peakMonth = monthlyData.reduce((best, item) => item.count > best.count ? item : best, monthlyData[0]);
  const yearTotal = monthlyData.reduce((total, item) => total + item.count, 0);
  const typeData = analysisStats.incidentsParType.slice(0, 8);
  const applicationData = analysisStats.incidentsParApplication.slice(0, 8);
  const activeList = activeTab === "type" ? typeData : applicationData;
  const activeTitle = analysisTabs.find((tab) => tab.id === activeTab)?.label ?? "Analyse";
  const exportFilename = `incidents-${activeTab}-${selectedYear ?? "toutes-annees"}.svg`;

  const criticCount = stats.incidentsParCriticite
    .filter((item) => ["critique", "fort", "haute"].includes(item.nom.toLowerCase()))
    .reduce((total, item) => total + item.count, 0);

  const cards = [
    { label: "Total incidents", value: stats.totalIncidents, icon: Layers3, color: "bg-coris-blue-soft text-coris-blue", accent: "bg-coris-blue" },
    { label: "En cours", value: stats.incidentsEnCours, icon: Clock3, color: "bg-amber-50 text-amber-600", accent: "bg-amber-500" },
    { label: "Clotures", value: stats.incidentsClotures, icon: CheckCircle2, color: "bg-emerald-50 text-emerald-600", accent: "bg-emerald-500" },
    { label: "Critiques / forts", value: criticCount, icon: ShieldAlert, color: "bg-white text-coris-red", accent: "bg-coris-red" },
  ];

  if (loading) return <div className="space-y-6"><div className="h-12 animate-pulse rounded-xl bg-coris-blue/10" /><div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">{[1, 2, 3, 4].map((item) => <div key={item} className="h-32 animate-pulse rounded-2xl bg-white shadow-sm" />)}</div></div>;
  if (error) return <div role="alert" className="rounded-2xl border border-red-200 bg-white p-4 text-sm text-red-700">{error}</div>;

  return (
    <div className="animate-fade-up space-y-4 sm:space-y-6">
      <header className="flex flex-col justify-between gap-4 border-b border-slate-200/80 pb-4 sm:flex-row sm:items-end sm:pb-5">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-slate-800 sm:text-3xl">Tableau de bord</h1>
          <p className="mt-1 text-sm text-slate-500">{dashboardTab === "incidents" ? "Incidents" : "Projets"}</p>
        </div>
        <Link href={dashboardTab === "incidents" ? "/incidents" : "/projets"} className="inline-flex w-full items-center justify-center gap-2 rounded-xl bg-coris-blue px-4 py-2.5 text-sm font-bold text-white shadow-lg shadow-blue-900/15 transition-transform hover:-translate-y-0.5 hover:bg-coris-navy sm:w-fit">Voir les {dashboardTab === "incidents" ? "incidents" : "projets"} <ArrowUpRight className="h-4 w-4" /></Link>
      </header>

      <div role="tablist" aria-label="Module du tableau de bord" className="grid grid-cols-2 rounded-xl bg-coris-blue p-1 text-xs font-bold shadow-md shadow-blue-900/20 sm:w-fit sm:min-w-[320px]">
        {([['incidents', 'Incidents'], ['projets', 'Projets']] as const).map(([id, label]) => (
          <button
            key={id}
            type="button"
            role="tab"
            aria-selected={dashboardTab === id}
            onClick={() => setDashboardTab(id)}
            className={`h-10 rounded-lg px-5 transition-all ${dashboardTab === id ? "bg-coris-navy text-white shadow-sm" : "text-white/90 hover:bg-white/10 hover:text-white"}`}
          >
            <span className="inline-flex items-center gap-2">{id === "projets" && <FolderKanban className="h-4 w-4" />}{label}</span>
          </button>
        ))}
      </div>

      {dashboardTab === "projets" ? <ProjectDashboardView stats={projectStats} recentProjects={recentProjects} /> : <>
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {cards.map(({ label, value, icon: Icon, color, accent }) => (
          <div key={label} className="hover-lift relative overflow-hidden rounded-xl border border-slate-200/80 bg-white p-4 shadow-[0_12px_35px_rgba(13,55,122,0.05)] sm:rounded-2xl sm:p-5">
            <span className={`absolute inset-y-0 left-0 w-1 ${accent}`} />
            <div className="flex items-start justify-between">
              <div>
                <p className="text-xs font-bold uppercase tracking-[0.1em] text-slate-400">{label}</p>
                <p className="mt-3 text-3xl font-bold tracking-tight text-slate-800">{value}</p>
              </div>
              <div className={`flex h-11 w-11 items-center justify-center rounded-2xl ${color}`}><Icon className="h-5 w-5" /></div>
            </div>
          </div>
        ))}
      </div>

      <div className="space-y-4 sm:space-y-6">
        <section className="rounded-xl border border-slate-200/80 bg-white p-4 shadow-[0_14px_45px_rgba(13,55,122,0.06)] sm:rounded-2xl sm:p-6">
          <div className="mb-6 flex items-center justify-between">
            <h2 className="text-xl font-bold text-slate-800">Derniers incidents</h2>
            <Link href="/incidents" className="inline-flex items-center gap-1 text-xs font-bold text-coris-blue hover:text-coris-red">Tout voir <ArrowUpRight className="h-3.5 w-3.5" /></Link>
          </div>
          <div className="space-y-2">
            {recentIncidents.map((incident) => (
              <Link key={incident.id} href={`/incidents/${incident.id}`} className="group flex min-w-0 items-center gap-2 rounded-xl border border-transparent p-2.5 transition-all hover:border-blue-100 hover:bg-coris-blue-soft/50 sm:gap-3 sm:p-3">
                <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-coris-blue-soft text-coris-blue sm:h-10 sm:w-10"><AlertCircle className="h-4 w-4" /></div>
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-bold text-slate-700 group-hover:text-coris-blue">{incident.intitule}</p>
                  <p className="mt-1 truncate text-xs text-slate-400">{incident.numero} - {incident.application.nom} - {formatDate(incident.dateDeclaration)}</p>
                </div>
                <div className="hidden shrink-0 items-end gap-2 sm:flex sm:flex-col">
                  <span className={statusClass(incident.statut.nom)}>{incident.statut.nom}</span>
                  <span className="text-[11px] text-slate-400">{formatDuration(incident.dureeMinutes, incident.dateDeclaration)}</span>
                </div>
                <ArrowUpRight className="h-4 w-4 shrink-0 text-slate-300 transition-transform group-hover:-translate-y-0.5 group-hover:translate-x-0.5 group-hover:text-coris-red" />
              </Link>
            ))}
            {recentIncidents.length === 0 && <div className="py-12 text-center text-sm text-slate-400">Aucun incident enregistre.</div>}
          </div>
        </section>

        <section className="rounded-xl border border-slate-200/80 bg-white p-4 shadow-[0_14px_45px_rgba(13,55,122,0.06)] sm:rounded-2xl sm:p-6">
          <div className="mb-5 flex flex-col gap-4">
            <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
              <div className="flex min-w-0 items-center gap-3">
                <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-white text-coris-red"><BarChart3 className="h-5 w-5" /></div>
                <div className="min-w-0">
                  <h2 className="text-lg font-bold text-slate-800 sm:text-xl">Analyse des incidents</h2>
                  <p className="mt-1 text-xs font-semibold text-slate-400">{selectedYear ? `${yearTotal} incident${yearTotal > 1 ? "s" : ""} en ${selectedYear}` : "Aucune annee disponible"}</p>
                </div>
              </div>
              <div className="grid grid-cols-2 gap-2 sm:flex sm:flex-wrap sm:items-center">
                <select
                  value={selectedYear ?? ""}
                  onChange={(event) => setSelectedYear(event.target.value ? Number(event.target.value) : null)}
                  className="h-9 w-full rounded-xl border border-slate-200 bg-white px-3 text-xs font-bold text-slate-700 outline-none transition-all focus:border-coris-blue focus:ring-4 focus:ring-coris-blue/10 sm:w-auto"
                  aria-label="Selectionner l'annee de l'analyse"
                >
                  {availableYears.length === 0 && <option value="">Aucune annee</option>}
                  {availableYears.map((year) => <option key={year} value={year}>{year}</option>)}
                </select>
                <button
                  type="button"
                  onClick={() => exportSvg(chartRef.current, exportFilename)}
                  disabled={selectedYear === null}
                  className="inline-flex h-9 w-full items-center justify-center gap-2 rounded-xl border border-slate-200 px-3 text-xs font-bold text-coris-blue transition-all hover:border-coris-blue hover:bg-coris-blue-soft disabled:cursor-not-allowed disabled:opacity-50 sm:w-auto"
                >
                  <Download className="h-3.5 w-3.5" />
                  Exporter
                </button>
              </div>
            </div>

            <div role="tablist" aria-label="Vues d'analyse" className="grid grid-cols-3 rounded-xl bg-slate-100 p-1 text-[11px] font-bold text-slate-500 sm:text-xs">
              {analysisTabs.map((tab) => (
                <button
                  key={tab.id}
                  type="button"
                  role="tab"
                  aria-selected={activeTab === tab.id}
                  onClick={() => setActiveTab(tab.id)}
                  className={`h-9 min-w-0 rounded-lg px-2 transition-all sm:px-3 ${activeTab === tab.id ? "bg-white text-coris-blue shadow-sm" : "hover:text-slate-800"}`}
                >
                  {tab.label}
                </button>
              ))}
            </div>
          </div>

          <div className="relative min-w-0 overflow-hidden">
            {analysisLoading && <div className="absolute inset-0 z-10 grid place-items-center rounded-xl bg-white/70 text-sm font-bold text-coris-blue">Chargement...</div>}
            {activeTab === "monthly" ? (
              <MonthlyChart refElement={chartRef} selectedYear={selectedYear} data={monthlyData} maxCount={maxMonthlyCount} peak={peakMonth} total={yearTotal} />
            ) : (
              <RankingChart refElement={chartRef} title={activeTitle} selectedYear={selectedYear} data={activeList} />
            )}
          </div>
        </section>
      </div>
      </>}
    </div>
  );
}

function ProjectDashboardView({ stats, recentProjects }: { stats: ProjectDashboardStats; recentProjects: ProjetWithDetails[] }) {
  const cards = [
    { label: "Total projets", value: stats.totalProjets, icon: FolderKanban, color: "bg-coris-blue-soft text-coris-blue", accent: "bg-coris-blue" },
    { label: "En cours", value: stats.projetsEnCours, icon: Clock3, color: "bg-amber-50 text-amber-600", accent: "bg-amber-500" },
    { label: "Planifiés", value: stats.projetsPlanifies, icon: CalendarCheck, color: "bg-violet-50 text-violet-600", accent: "bg-violet-500" },
    { label: "Clôturés", value: stats.projetsClotures, icon: CheckCircle2, color: "bg-emerald-50 text-emerald-600", accent: "bg-emerald-500" },
  ];
  const maxDomainCount = Math.max(...stats.projetsParDomaine.map((item) => item.count), 1);
  return <div className="space-y-4 sm:space-y-6"><div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">{cards.map(({ label, value, icon: Icon, color, accent }) => <div key={label} className="hover-lift relative overflow-hidden rounded-xl border border-slate-200/80 bg-white p-4 shadow-[0_12px_35px_rgba(13,55,122,0.05)] sm:rounded-2xl sm:p-5"><span className={`absolute inset-y-0 left-0 w-1 ${accent}`} /><div className="flex items-start justify-between"><div><p className="text-xs font-bold uppercase tracking-[0.1em] text-slate-400">{label}</p><p className="mt-3 text-3xl font-bold tracking-tight text-slate-800">{value}</p></div><div className={`flex h-11 w-11 items-center justify-center rounded-2xl ${color}`}><Icon className="h-5 w-5" /></div></div></div>)}</div><div className="grid gap-4 lg:grid-cols-[1.15fr_.85fr]"><section className="rounded-xl border border-slate-200/80 bg-white p-4 shadow-[0_14px_45px_rgba(13,55,122,0.06)] sm:rounded-2xl sm:p-6"><div className="flex items-center justify-between"><div><h2 className="text-xl font-bold text-slate-800">Avancement moyen</h2><p className="mt-1 text-xs text-slate-400">Vision globale du portefeuille projets</p></div><p className="text-3xl font-bold text-coris-blue">{Math.round(stats.tauxMoyen * 100)}%</p></div><div className="mt-6 h-3 overflow-hidden rounded-full bg-slate-100"><div className="h-full rounded-full bg-coris-blue" style={{ width: `${Math.round(stats.tauxMoyen * 100)}%` }} /></div><h3 className="mt-8 text-sm font-bold text-slate-700">Projets par domaine</h3><div className="mt-4 space-y-3">{stats.projetsParDomaine.slice(0, 8).map((item) => <div key={item.id}><div className="mb-1 flex justify-between gap-3 text-xs"><span className="truncate font-semibold text-slate-600">{item.nom}</span><span className="font-bold text-coris-blue">{item.count}</span></div><div className="h-2 rounded-full bg-slate-100"><div className="h-full rounded-full bg-coris-blue/80" style={{ width: `${(item.count / maxDomainCount) * 100}%` }} /></div></div>)}{stats.projetsParDomaine.length === 0 && <p className="py-8 text-center text-sm text-slate-400">Aucun projet enregistré.</p>}</div></section><section className="rounded-xl border border-slate-200/80 bg-white p-4 shadow-[0_14px_45px_rgba(13,55,122,0.06)] sm:rounded-2xl sm:p-6"><div className="mb-5 flex items-center justify-between"><h2 className="text-xl font-bold text-slate-800">Derniers projets</h2><Link href="/projets" className="inline-flex items-center gap-1 text-xs font-bold text-coris-blue hover:text-coris-red">Tout voir <ArrowUpRight className="h-3.5 w-3.5" /></Link></div><div className="space-y-2">{recentProjects.map((project) => <Link key={project.id} href={`/projets/${project.id}`} className="group flex items-center gap-3 rounded-xl border border-transparent p-3 transition-all hover:border-blue-100 hover:bg-coris-blue-soft/50"><div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-coris-blue-soft text-coris-blue"><FolderKanban className="h-4 w-4" /></div><div className="min-w-0 flex-1"><p className="truncate text-sm font-bold text-slate-700 group-hover:text-coris-blue">{project.nom}</p><p className="mt-1 truncate text-xs text-slate-400">{project.numero} · {project.domaineProjet?.nom}</p></div><div className="hidden text-right sm:block"><p className="text-sm font-bold text-coris-blue">{Math.round(project.tauxAvancement * 100)}%</p><span className={statusClass(project.statutProjet?.nom || "")}>{project.statutProjet?.nom}</span></div><ArrowUpRight className="h-4 w-4 shrink-0 text-slate-300 group-hover:text-coris-red" /></Link>)}{recentProjects.length === 0 && <div className="py-12 text-center text-sm text-slate-400">Aucun projet enregistré.</div>}</div></section></div></div>;
}

function MonthlyChart({
  refElement,
  selectedYear,
  data,
  maxCount,
  peak,
  total,
}: {
  refElement: React.RefObject<SVGSVGElement | null>;
  selectedYear: number | null;
  data: { label: string; month: number; count: number }[];
  maxCount: number;
  peak: { label: string; count: number };
  total: number;
}) {
  return (
    <svg ref={refElement} viewBox="0 0 760 300" role="img" aria-label={`Histogramme des incidents par mois ${selectedYear ?? ""}`} className="h-auto w-full max-w-full">
      <rect x="0" y="0" width="760" height="300" rx="18" fill="#ffffff" />
      <text x="42" y="28" fill="#1f2937" fontSize="16" fontWeight="700">Incidents par mois {selectedYear ?? ""}</text>
      <text x="42" y="49" fill="#64748b" fontSize="12">Pic: {peak.label} ({peak.count}) - Total: {total}</text>
      {[0, 1, 2, 3].map((line) => {
        const y = 235 - line * 50;
        return <g key={line}><line x1="44" y1={y} x2="728" y2={y} stroke="#e2e8f0" strokeWidth="1" /><text x="18" y={y + 4} fill="#94a3b8" fontSize="10">{Math.round((maxCount / 3) * line)}</text></g>;
      })}
      {data.map((item, index) => {
        const slot = 684 / 12;
        const barWidth = 34;
        const height = Math.max((item.count / maxCount) * 170, item.count ? 8 : 0);
        const x = 52 + index * slot + (slot - barWidth) / 2;
        const y = 235 - height;
        const isPeak = item.count > 0 && item.count === peak.count;

        return (
          <g key={item.month}>
            <rect x={x} y={y} width={barWidth} height={height} rx="7" fill={isPeak ? "#dc2626" : "#2f428f"} />
            <text x={x + barWidth / 2} y={Math.max(y - 8, 67)} textAnchor="middle" fill="#1f2937" fontSize="11" fontWeight="700">{item.count}</text>
            <text x={x + barWidth / 2} y="258" textAnchor="middle" fill="#64748b" fontSize="11" fontWeight="700">{item.label}</text>
          </g>
        );
      })}
    </svg>
  );
}

function RankingChart({
  refElement,
  title,
  selectedYear,
  data,
}: {
  refElement: React.RefObject<SVGSVGElement | null>;
  title: string;
  selectedYear: number | null;
  data: DashboardCount[];
}) {
  const maxCount = Math.max(...data.map((item) => item.count), 1);
  const total = data.reduce((sum, item) => sum + item.count, 0);

  return (
    <svg ref={refElement} viewBox="0 0 760 360" role="img" aria-label={`${title} ${selectedYear ?? ""}`} className="h-auto w-full max-w-full">
      <rect x="0" y="0" width="760" height="360" rx="18" fill="#ffffff" />
      <text x="42" y="30" fill="#1f2937" fontSize="16" fontWeight="700">{title} {selectedYear ?? ""}</text>
      <text x="42" y="51" fill="#64748b" fontSize="12">Total affiche: {total}</text>
      {data.length === 0 && <text x="300" y="180" fill="#94a3b8" fontSize="13" fontWeight="700">Aucune donnee disponible</text>}
      {data.map((item, index) => {
        const y = 82 + index * 33;
        const width = Math.max((item.count / maxCount) * 470, item.count ? 10 : 0);
        const label = item.nom.length > 32 ? `${item.nom.slice(0, 29)}...` : item.nom;

        return (
          <g key={item.id}>
            <text x="42" y={y + 14} fill="#475569" fontSize="12" fontWeight="700">{label}</text>
            <rect x="260" y={y + 3} width="430" height="12" rx="6" fill="#eef2f7" />
            <rect x="260" y={y + 3} width={width} height="12" rx="6" fill={index === 0 ? "#dc2626" : "#2f428f"} />
            <text x="710" y={y + 14} fill="#1f2937" fontSize="12" fontWeight="700" textAnchor="end">{item.count}</text>
          </g>
        );
      })}
    </svg>
  );
}
