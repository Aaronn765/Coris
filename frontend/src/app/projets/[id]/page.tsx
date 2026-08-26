"use client";

import { use, useEffect, useState } from "react";
import Link from "next/link";
import { ArrowLeft, CalendarDays, CheckCircle2, Edit3, FolderKanban, Users } from "lucide-react";
import { apiService } from "@/services/api";
import { ProjetWithDetails } from "@/types";
import { ProjectForm } from "@/components/projects/ProjectForm";
import { Button } from "@/components/ui/button";
import { formatDate } from "@/lib/formatters";

const clean = (value: string) => value.normalize("NFD").replace(/[\u0300-\u036f]/g, "").toLowerCase();
const percent = (value: number) => Math.max(0, Math.min(100, Math.round(value * 100)));

function statusClass(value: string) {
  const name = clean(value);
  if (name.includes("clotur")) return "coris-badge coris-badge-success";
  if (name.includes("suspend")) return "coris-badge coris-badge-danger";
  if (name.includes("en cours")) return "coris-badge coris-badge-warning";
  return "coris-badge coris-badge-info";
}

export default function ProjetDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const [project, setProject] = useState<ProjetWithDetails | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    apiService.getProjetById(Number(id)).then(setProject).catch((reason: Error) => setError(reason.message)).finally(() => setLoading(false));
  }, [id]);

  if (loading) return <div className="space-y-5"><div className="h-48 animate-pulse rounded-2xl bg-coris-blue/10" /><div className="h-96 animate-pulse rounded-2xl bg-white" /></div>;
  if (error) return <div role="alert" className="rounded-2xl border border-red-200 bg-red-50 p-5 text-sm text-red-700">{error}</div>;
  if (!project) return <div className="rounded-2xl border border-red-200 bg-red-50 p-5 text-red-700">Projet introuvable.</div>;

  const completedSteps = project.etapes.filter((step) => clean(step.statutEtape?.nom || "").includes("termin")).length;
  return <div className="animate-fade-up mx-auto max-w-6xl space-y-4 sm:space-y-6"><header className="relative overflow-hidden rounded-2xl bg-coris-gradient px-4 py-5 text-white shadow-[0_22px_55px_rgba(8,54,130,0.2)] sm:px-8 sm:py-8"><div className="coris-hero-orb coris-hero-orb-one" /><div className="relative flex flex-col gap-4 sm:flex-row sm:items-start"><Link href="/projets"><Button variant="outline" size="icon" className="shrink-0 border-white/25 bg-white/10 text-white hover:bg-white hover:text-coris-blue"><ArrowLeft className="h-4 w-4" /></Button></Link><div className="min-w-0 flex-1"><p className="font-mono text-xs font-bold tracking-[0.18em] text-blue-100">{project.numero}</p><h1 className="mt-2 max-w-4xl break-words text-2xl font-bold leading-tight sm:text-3xl">{project.nom}</h1><p className="mt-3 text-sm text-blue-100">{project.domaineProjet?.nom || "Domaine non renseigné"}</p></div><span className={statusClass(project.statutProjet?.nom || "")}>{project.statutProjet?.nom || "—"}</span></div></header>
    <section className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4"><Summary icon={FolderKanban} label="Avancement" value={`${percent(project.tauxAvancement)}%`} /><Summary icon={CheckCircle2} label="Étapes terminées" value={`${completedSteps} / ${project.etapes.length}`} /><Summary icon={CalendarDays} label="Échéance" value={formatDate(project.dateEcheance)} /><Summary icon={Users} label="Responsables" value={String(project.responsables.length)} /></section>
    <section className="rounded-2xl border border-slate-200/80 bg-white p-4 shadow-[0_14px_45px_rgba(13,55,122,0.06)] sm:p-6"><div className="flex items-start justify-between gap-3"><div><h2 className="text-xl font-bold text-slate-800">Suivi du projet</h2><p className="mt-1 text-xs text-slate-400">Progression globale et état des étapes</p></div><Edit3 className="h-5 w-5 text-coris-blue" /></div><div className="mt-5 h-3 overflow-hidden rounded-full bg-slate-100"><div className="h-full rounded-full bg-coris-blue" style={{ width: `${percent(project.tauxAvancement)}%` }} /></div><div className="mt-6 space-y-3">{project.etapes.map((step) => <div key={step.id} className="rounded-xl border border-slate-100 bg-slate-50/70 p-3 sm:p-4"><div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between"><div className="min-w-0"><p className="break-words text-sm font-bold text-slate-700">{step.ordre + 1}. {step.nom}</p><p className="mt-1 text-xs text-slate-400">{formatDate(step.dateDebut)} {step.dateFin ? `→ ${formatDate(step.dateFin)}` : ""}</p></div><div className="flex shrink-0 items-center gap-2"><span className="text-xs font-bold text-coris-blue">{percent(step.tauxAvancement)}%</span><span className={statusClass(step.statutEtape?.nom || "")}>{step.statutEtape?.nom || "—"}</span></div></div><div className="mt-3 h-1.5 overflow-hidden rounded-full bg-slate-200"><div className="h-full rounded-full bg-coris-blue/80" style={{ width: `${percent(step.tauxAvancement)}%` }} /></div></div>)}{project.etapes.length === 0 && <div className="rounded-xl border border-dashed border-slate-200 px-4 py-8 text-center text-sm text-slate-400">Aucune étape enregistrée.</div>}</div></section>
    <section className="grid gap-4 lg:grid-cols-2"><InfoBlock title="Description" value={project.description} /><InfoBlock title="Responsables" value={project.responsables.map((responsible) => responsible.nom).join(" · ")} /><InfoBlock title="Support" value={project.support} /><InfoBlock title="Acteurs métiers" value={project.acteursMetiers} /><InfoBlock title="Contraintes" value={project.contraintes} /><InfoBlock title="Commentaires" value={project.commentaires} /></section>
    <div className="flex items-end justify-between gap-3 px-1"><h2 className="text-xl font-bold text-slate-800">Modifier le projet</h2><p className="hidden text-xs text-slate-400 sm:block">Dernière modification : {formatDate(project.updatedAt)}</p></div><ProjectForm initialData={project} />
  </div>;
}

function Summary({ icon: Icon, label, value }: { icon: typeof FolderKanban; label: string; value: string }) { return <div className="flex items-center gap-3 rounded-2xl border border-slate-200/80 bg-white p-4 shadow-[0_10px_30px_rgba(13,55,122,0.05)]"><div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-coris-blue-soft text-coris-blue"><Icon className="h-4 w-4" /></div><div className="min-w-0"><p className="text-[10px] font-bold uppercase tracking-[0.12em] text-slate-400">{label}</p><p className="mt-1 truncate text-sm font-bold text-slate-700">{value}</p></div></div>; }
function InfoBlock({ title, value }: { title: string; value?: string }) { return <div className="rounded-2xl border border-slate-200/80 bg-white p-4 shadow-sm sm:p-5"><p className="text-[10px] font-bold uppercase tracking-[0.12em] text-slate-400">{title}</p><p className="mt-2 whitespace-pre-wrap break-words text-sm leading-6 text-slate-600">{value || "Non renseigné"}</p></div>; }
