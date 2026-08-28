"use client";

import { use, useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { ArrowLeft, CalendarDays, CheckCircle2, Download, FolderKanban, ListChecks, Loader2, Plus, Save, Users, X } from "lucide-react";
import { apiService } from "@/services/api";
import { ProjetWithDetails, StatutEtape } from "@/types";
import { ProjectInfoEditor } from "@/components/projects/ProjectInfoEditor";
import { ProjectStepCard } from "@/components/projects/ProjectStepCard";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { formatDate } from "@/lib/formatters";
import {
  buildProjetPayload,
  clean,
  newStepDraft,
  progress,
  readStepSaveTimes,
  statusClass,
  StepDraft,
  toStepDraft,
  writeStepSaveTime,
} from "@/lib/project-steps";

export default function ProjetDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const [project, setProject] = useState<ProjetWithDetails | null>(null);
  const [stepStatuses, setStepStatuses] = useState<StatutEtape[]>([]);
  const [expandedIndex, setExpandedIndex] = useState<number | null>(null);
  const [stepSaveTimes, setStepSaveTimes] = useState<Record<number, string>>({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [addingStep, setAddingStep] = useState(false);
  const [newStep, setNewStep] = useState<StepDraft | null>(null);
  const [savingNewStep, setSavingNewStep] = useState(false);
  const [exportingPdf, setExportingPdf] = useState(false);
  const [newStepError, setNewStepError] = useState<string | null>(null);

  useEffect(() => {
    Promise.all([
      apiService.getProjetById(Number(id)),
      apiService.getStatutsEtape(),
    ])
      .then(([loadedProject, loadedStepStatuses]) => {
        if (!loadedProject) {
          setError("Projet introuvable.");
          return;
        }
        setProject(loadedProject);
        setStepStatuses(loadedStepStatuses);
        setStepSaveTimes(readStepSaveTimes(loadedProject.id));
      })
      .catch((reason: Error) => setError(reason.message))
      .finally(() => setLoading(false));
  }, [id]);

  const completedSteps = useMemo(
    () => project?.etapes.filter((step) => clean(step.statutEtape?.nom || "").includes("termin")).length ?? 0,
    [project],
  );

  const handleStepSaved = (updated: ProjetWithDetails, order: number, savedAt: string) => {
    setProject(updated);
    setStepSaveTimes((current) => ({ ...current, [order]: savedAt }));
    writeStepSaveTime(updated.id, order, savedAt);
  };

  const toggleStep = (index: number) => {
    setExpandedIndex((current) => (current === index ? null : index));
    setAddingStep(false);
    setNewStep(null);
  };

  const startAddStep = () => {
    setExpandedIndex(null);
    setAddingStep(true);
    setNewStep(newStepDraft(stepStatuses));
    setNewStepError(null);
  };

  const cancelAddStep = () => {
    setAddingStep(false);
    setNewStep(null);
    setNewStepError(null);
  };

  const saveNewStep = async () => {
    if (!project || !newStep) return;
    if (!newStep.nom.trim()) {
      setNewStepError("Le libellé de l'étape est obligatoire.");
      return;
    }

    setSavingNewStep(true);
    setNewStepError(null);
    try {
      const allDrafts = [...project.etapes.map(toStepDraft), newStep];
      const updated = await apiService.updateProjet(project.id, buildProjetPayload(project, allDrafts));
      if (!updated) throw new Error("Le projet n'existe plus.");
      const savedAt = new Date().toISOString();
      const newOrder = updated.etapes.length - 1;
      writeStepSaveTime(updated.id, newOrder, savedAt);
      setStepSaveTimes((current) => ({ ...current, [newOrder]: savedAt }));
      setProject(updated);
      setAddingStep(false);
      setNewStep(null);
      setExpandedIndex(updated.etapes.length - 1);
    } catch (reason) {
      setNewStepError(reason instanceof Error ? reason.message : "L'ajout de l'étape a échoué.");
    } finally {
      setSavingNewStep(false);
    }
  };

  const handleExportPdf = async () => {
    if (!project) return;
    setExportingPdf(true);
    try {
      const blob = await apiService.exportProjetPdf(project.id);
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement("a");
      anchor.href = url;
      anchor.download = `${project.numero}-fiche.pdf`;
      anchor.rel = "noopener";
      anchor.style.display = "none";
      document.body.appendChild(anchor);
      anchor.click();
      anchor.remove();
      window.setTimeout(() => URL.revokeObjectURL(url), 1000);
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "L'export PDF a échoué.");
    } finally {
      setExportingPdf(false);
    }
  };

  if (loading) {
    return (
      <div className="space-y-5">
        <div className="h-48 animate-pulse rounded-2xl bg-coris-blue/10" />
        <div className="h-96 animate-pulse rounded-2xl bg-white" />
      </div>
    );
  }

  if (error) {
    return <div role="alert" className="rounded-2xl border border-red-200 bg-white p-5 text-sm text-red-700">{error}</div>;
  }

  if (!project) {
    return <div className="rounded-2xl border border-red-200 bg-white p-5 text-red-700">Projet introuvable.</div>;
  }

  const globalPercent = progress(project.tauxAvancement);

  return (
    <div className="animate-fade-up mx-auto max-w-6xl space-y-4 sm:space-y-6">
      <header className="relative overflow-hidden rounded-2xl bg-coris-gradient px-4 py-5 text-white shadow-[0_22px_55px_rgba(8,54,130,0.2)] sm:px-8 sm:py-8">
        <div className="coris-hero-orb coris-hero-orb-one" />
        <div className="relative flex flex-col gap-4 sm:flex-row sm:items-start">
          <Link href="/projets">
            <Button variant="outline" size="icon" className="shrink-0 border-white/25 bg-white/10 text-white hover:bg-white hover:text-coris-blue">
              <ArrowLeft className="h-4 w-4" />
            </Button>
          </Link>
          <div className="min-w-0 flex-1">
            <p className="font-mono text-xs font-bold tracking-[0.18em] text-blue-100">{project.numero}</p>
            <h1 className="mt-2 max-w-4xl break-words text-2xl font-bold leading-tight sm:text-3xl">{project.nom}</h1>
            <p className="mt-3 text-sm text-blue-100">{project.domaineProjet?.nom || "Domaine non renseigné"}</p>
          </div>
          <div className="flex shrink-0 flex-wrap items-center gap-2 sm:flex-col sm:items-end">
            <span className={statusClass(project.statutProjet?.nom || "")}>{project.statutProjet?.nom || "—"}</span>
            <Button type="button" onClick={handleExportPdf} disabled={exportingPdf} variant="outline" className="h-9 rounded-xl border-white/25 bg-white/10 text-xs text-white hover:bg-white hover:text-coris-blue">
              {exportingPdf ? <Loader2 className="mr-2 h-3.5 w-3.5 animate-spin" /> : <Download className="mr-2 h-3.5 w-3.5" />}
              Exporter PDF
            </Button>
          </div>
        </div>
      </header>

      <section className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
        <Summary icon={FolderKanban} label="Avancement" value={`${globalPercent}%`} />
        <Summary icon={CheckCircle2} label="Étapes terminées" value={`${completedSteps} / ${project.etapes.length}`} />
        <Summary icon={CalendarDays} label="Échéance" value={formatDate(project.dateEcheance)} />
        <Summary icon={Users} label="Responsables" value={String(project.responsables.length)} />
      </section>

      <section className="rounded-2xl border border-slate-200/80 bg-white p-4 shadow-[0_14px_45px_rgba(13,55,122,0.06)] sm:p-6">
        <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <div className="flex items-center gap-2">
              <ListChecks className="h-5 w-5 text-coris-blue" />
              <h2 className="text-xl font-bold text-slate-800">Suivi du projet</h2>
            </div>
            <p className="mt-1 text-xs text-slate-400">
              Cliquez sur une étape pour voir le détail et la modifier individuellement.
            </p>
          </div>
        </div>

        <div className="mt-5 h-3 overflow-hidden rounded-full bg-slate-100">
          <div className="h-full rounded-full bg-coris-blue transition-all duration-500" style={{ width: `${globalPercent}%` }} />
        </div>

        <div className="mt-6 space-y-3">
          {project.etapes.map((step, index) => (
            <ProjectStepCard
              key={`${step.id}-${step.ordre}`}
              step={step}
              index={index}
              project={project}
              stepStatuses={stepStatuses}
              expanded={expandedIndex === index}
              lastSavedAt={stepSaveTimes[step.ordre]}
              onToggle={() => toggleStep(index)}
              onSaved={handleStepSaved}
            />
          ))}

          {project.etapes.length === 0 && !addingStep && (
            <div className="rounded-xl border border-dashed border-slate-200 px-4 py-10 text-center">
              <p className="text-sm text-slate-400">Aucune étape enregistrée pour ce projet.</p>
              <Button
                type="button"
                variant="outline"
                onClick={startAddStep}
                className="mt-4 h-9 rounded-xl border-coris-blue/30 text-xs text-coris-blue hover:bg-coris-blue-soft"
              >
                <Plus className="mr-2 h-3.5 w-3.5" />
                Ajouter la première étape
              </Button>
            </div>
          )}

          {addingStep && newStep && (
            <div className="rounded-xl border border-slate-200 border-l-4 border-l-coris-blue bg-white p-4">
              <p className="text-sm font-bold text-slate-800">Nouvelle étape</p>
              <div className="mt-4 space-y-3">
                <div className="grid gap-3 md:grid-cols-[minmax(0,1fr)_100px_180px]">
                  <div className="space-y-1.5">
                    <Label className="text-xs">Libellé *</Label>
                    <Input value={newStep.nom} onChange={(event) => setNewStep({ ...newStep, nom: event.target.value })} />
                  </div>
                  <div className="space-y-1.5">
                    <Label className="text-xs">Taux (%)</Label>
                    <Input
                      type="number"
                      min="0"
                      max="100"
                      value={newStep.tauxAvancement}
                      onChange={(event) => setNewStep({ ...newStep, tauxAvancement: event.target.value })}
                    />
                  </div>
                  <div className="space-y-1.5">
                    <Label className="text-xs">Statut *</Label>
                    <select
                      value={newStep.statutEtapeId || ""}
                      onChange={(event) => setNewStep({ ...newStep, statutEtapeId: Number(event.target.value) })}
                      className="h-10 w-full rounded-md border border-slate-200 bg-white px-3 text-sm text-slate-700 outline-none focus:border-coris-blue focus:ring-4 focus:ring-coris-blue/10"
                    >
                      {stepStatuses.map((status) => (
                        <option key={status.id} value={status.id}>{status.nom}</option>
                      ))}
                    </select>
                  </div>
                </div>
                <div className="grid gap-3 sm:grid-cols-2">
                  <div className="space-y-1.5">
                    <Label className="text-xs">Date de début</Label>
                    <Input type="date" value={newStep.dateDebut} onChange={(event) => setNewStep({ ...newStep, dateDebut: event.target.value })} />
                  </div>
                  <div className="space-y-1.5">
                    <Label className="text-xs">Date de fin</Label>
                    <Input type="date" value={newStep.dateFin} onChange={(event) => setNewStep({ ...newStep, dateFin: event.target.value })} />
                  </div>
                </div>
                <div className="space-y-1.5">
                  <Label className="text-xs">Commentaires</Label>
                  <Textarea
                    value={newStep.commentaires}
                    onChange={(event) => setNewStep({ ...newStep, commentaires: event.target.value })}
                    className="min-h-16"
                  />
                </div>
                {newStepError && <p role="alert" className="text-xs text-red-600">{newStepError}</p>}
                <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
                  <Button type="button" variant="outline" onClick={cancelAddStep} disabled={savingNewStep} className="h-9 rounded-xl text-xs">
                    <X className="mr-2 h-3.5 w-3.5" />
                    Annuler
                  </Button>
                  <Button type="button" onClick={saveNewStep} disabled={savingNewStep} className="h-9 rounded-xl bg-coris-blue text-xs text-white hover:bg-coris-blue/90">
                    <Save className="mr-2 h-3.5 w-3.5" />
                    {savingNewStep ? "Enregistrement..." : "Créer l'étape"}
                  </Button>
                </div>
              </div>
            </div>
          )}
        </div>

        {project.etapes.length > 0 && !addingStep && (
          <Button
            type="button"
            variant="outline"
            onClick={startAddStep}
            className="mt-4 h-9 w-full rounded-xl border-dashed border-coris-blue/30 text-xs text-coris-blue hover:bg-coris-blue-soft sm:w-auto"
          >
            <Plus className="mr-2 h-3.5 w-3.5" />
            Ajouter une étape
          </Button>
        )}
      </section>

      <ProjectInfoEditor project={project} onSaved={setProject} />
    </div>
  );
}

function Summary({ icon: Icon, label, value }: { icon: typeof FolderKanban; label: string; value: string }) {
  return (
    <div className="flex items-center gap-3 rounded-2xl border border-slate-200/80 bg-white p-4 shadow-[0_10px_30px_rgba(13,55,122,0.05)]">
      <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-coris-blue-soft text-coris-blue">
        <Icon className="h-4 w-4" />
      </div>
      <div className="min-w-0">
        <p className="text-[10px] font-bold uppercase tracking-[0.12em] text-slate-400">{label}</p>
        <p className="mt-1 truncate text-sm font-bold text-slate-700">{value}</p>
      </div>
    </div>
  );
}
