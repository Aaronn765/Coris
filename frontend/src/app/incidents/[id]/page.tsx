"use client";

import { use, useEffect, useState } from "react";
import Link from "next/link";
import { ArrowLeft, CalendarDays, Clock3, Layers3, ShieldAlert } from "lucide-react";
import { apiService } from "@/services/api";
import { IncidentWithDetails } from "@/types";
import { IncidentForm } from "@/components/incidents/IncidentForm";
import { Button } from "@/components/ui/button";
import { formatDate, formatDuration } from "@/lib/formatters";

const clean = (value: string) => value.normalize("NFD").replace(/[\u0300-\u036f]/g, "").toLowerCase();

function statusClass(value: string) {
  return ["resolu", "cloture", "ferme"].includes(clean(value))
    ? "coris-badge coris-badge-success"
    : "coris-badge coris-badge-warning";
}

function criticalityClass(value: string) {
  return ["critique", "fort", "haute"].includes(clean(value))
    ? "coris-badge coris-badge-danger"
    : "coris-badge coris-badge-info";
}

export default function EditIncidentPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const [incident, setIncident] = useState<IncidentWithDetails | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    apiService.getIncidentById(Number(id)).then((data) => {
      setIncident(data);
      setLoading(false);
    });
  }, [id]);

  if (loading) return <div className="space-y-5"><div className="h-44 animate-pulse rounded-xl bg-coris-blue/10 sm:rounded-[2rem]" /><div className="h-96 animate-pulse rounded-xl bg-white sm:rounded-[1.5rem]" /></div>;
  if (!incident) return <div className="rounded-2xl border border-red-200 bg-white p-5 text-red-700">Incident introuvable.</div>;

  const summary = [
    { label: "Statut", value: incident.statut?.nom || "—", icon: Layers3, badge: statusClass(incident.statut?.nom || "") },
    { label: "Criticité", value: incident.criticite?.nom || "—", icon: ShieldAlert, badge: criticalityClass(incident.criticite?.nom || "") },
    { label: "Durée", value: formatDuration(incident.dureeMinutes, incident.dateDeclaration), icon: Clock3 },
    { label: "Déclaration", value: formatDate(incident.dateDeclaration), icon: CalendarDays },
  ];

  return (
    <div className="animate-fade-up mx-auto max-w-6xl space-y-4 sm:space-y-6">
      <header className="relative overflow-hidden rounded-xl bg-coris-gradient px-4 py-5 text-white shadow-[0_22px_55px_rgba(8,54,130,0.2)] sm:rounded-[2rem] sm:px-8 sm:py-8">
        <div className="coris-hero-orb coris-hero-orb-one" />
        <div className="relative flex flex-col gap-4 sm:flex-row sm:items-start sm:gap-5">
          <Link href="/incidents"><Button variant="outline" size="icon" className="shrink-0 border-white/25 bg-white/10 text-white hover:bg-white hover:text-coris-blue"><ArrowLeft className="h-4 w-4" /></Button></Link>
          <div className="min-w-0 flex-1"><p className="font-mono text-xs font-bold tracking-[0.18em] text-blue-100">{incident.numero}</p><h1 className="mt-2 max-w-4xl break-words text-2xl font-bold leading-tight sm:text-3xl">{incident.intitule}</h1><p className="mt-3 text-sm text-blue-100">{incident.application?.nom || "Application non renseignée"} · {incident.typeIncident?.nom || "Type non renseigné"} · {incident.entite?.nom || "Entité non renseignée"}</p></div>
          <span className={statusClass(incident.statut?.nom || "")}>{incident.statut?.nom || "—"}</span>
        </div>
      </header>

      <section className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
        {summary.map(({ label, value, icon: Icon, badge }) => <div key={label} className="hover-lift flex items-center gap-3 rounded-xl border border-slate-200/80 bg-white p-4 shadow-[0_10px_30px_rgba(13,55,122,0.05)] sm:rounded-2xl"><div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-coris-blue-soft text-coris-blue"><Icon className="h-4 w-4" /></div><div className="min-w-0"><p className="text-[10px] font-bold uppercase tracking-[0.12em] text-slate-400">{label}</p>{badge ? <span className={`mt-1 ${badge}`}>{value}</span> : <p className="mt-1 truncate text-sm font-bold text-slate-700">{value}</p>}</div></div>)}
      </section>

      <div className="flex flex-wrap items-end justify-between gap-3 px-1"><h2 className="text-xl font-bold text-slate-800">Modifier l&apos;incident</h2><p className="text-xs text-slate-400">Dernière modification : {formatDate(incident.updatedAt || incident.dateDeclaration)}</p></div>
      <IncidentForm initialData={incident} />
    </div>
  );
}
