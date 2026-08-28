"use client";

import { useState } from "react";
import { CheckCircle2, ChevronDown, Clock, Pencil, Save, X } from "lucide-react";
import { apiService } from "@/services/api";
import { EtapeProjet, ProjetWithDetails, StatutEtape } from "@/types";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { formatDate, formatDateTime } from "@/lib/formatters";
import {
  buildProjetPayload,
  progress,
  statusClass,
  stepAccentClass,
  StepDraft,
  toStepDraft,
  writeStepSaveTime,
} from "@/lib/project-steps";

interface ProjectStepCardProps {
  step: EtapeProjet;
  index: number;
  project: ProjetWithDetails;
  stepStatuses: StatutEtape[];
  expanded: boolean;
  lastSavedAt?: string;
  onToggle: () => void;
  onSaved: (project: ProjetWithDetails, order: number, savedAt: string) => void;
}

export function ProjectStepCard({
  step,
  index,
  project,
  stepStatuses,
  expanded,
  lastSavedAt,
  onToggle,
  onSaved,
}: ProjectStepCardProps) {
  const [editing, setEditing] = useState(false);
  const [draft, setDraft] = useState<StepDraft>(() => toStepDraft(step));
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [savedFlash, setSavedFlash] = useState(false);

  const stepPercent = progress(step.tauxAvancement);
  const statusName = step.statutEtape?.nom || "—";

  const updateDraft = (changes: Partial<StepDraft>) => {
    setDraft((current) => ({ ...current, ...changes }));
  };

  const cancelEdit = () => {
    setDraft(toStepDraft(step));
    setEditing(false);
    setError(null);
  };

  const startEditing = () => {
    setDraft(toStepDraft(step));
    setError(null);
    setEditing(true);
  };

  const handleToggle = () => {
    if (expanded) {
      setEditing(false);
      setError(null);
    }
    onToggle();
  };

  const saveStep = async () => {
    if (!draft.nom.trim()) {
      setError("Le libellé de l'étape est obligatoire.");
      return;
    }
    if (!draft.statutEtapeId) {
      setError("Le statut de l'étape est obligatoire.");
      return;
    }

    setSaving(true);
    setError(null);
    try {
      const allDrafts = project.etapes.map((item, itemIndex) =>
        itemIndex === index ? draft : toStepDraft(item),
      );
      const updated = await apiService.updateProjet(project.id, buildProjetPayload(project, allDrafts));
      if (!updated) throw new Error("Le projet n'existe plus.");
      const savedAt = new Date().toISOString();
      writeStepSaveTime(project.id, step.ordre, savedAt);
      onSaved(updated, step.ordre, savedAt);
      setEditing(false);
      setSavedFlash(true);
      window.setTimeout(() => setSavedFlash(false), 2500);
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "La sauvegarde de l'étape a échoué.");
    } finally {
      setSaving(false);
    }
  };

  return (
    <article
      className={`overflow-hidden rounded-xl border bg-white transition-all duration-200 ${
        expanded
          ? "border-coris-blue/25 shadow-[0_8px_28px_rgba(13,55,122,0.08)]"
          : "border-slate-100 hover:border-coris-blue/20 hover:shadow-sm"
      } border-l-4 ${stepAccentClass(statusName)}`}
    >
      <button
        type="button"
        onClick={handleToggle}
        className="flex w-full items-start gap-3 p-3 text-left sm:p-4"
        aria-expanded={expanded}
      >
        <span className="mt-0.5 flex h-7 w-7 shrink-0 items-center justify-center rounded-lg bg-coris-blue-soft text-xs font-bold text-coris-blue">
          {index + 1}
        </span>
        <div className="min-w-0 flex-1">
          <div className="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
            <div className="min-w-0">
              <p className="break-words text-sm font-bold text-slate-800">{step.nom}</p>
              <p className="mt-1 text-xs text-slate-400">
                {formatDate(step.dateDebut)}
                {step.dateFin ? ` → ${formatDate(step.dateFin)}` : ""}
              </p>
            </div>
            <div className="flex shrink-0 flex-wrap items-center gap-2">
              {savedFlash && (
                <span className="inline-flex items-center gap-1 text-[11px] font-semibold text-emerald-600">
                  <CheckCircle2 className="h-3.5 w-3.5" />
                  Enregistré
                </span>
              )}
              <span className="text-xs font-bold text-coris-blue">{stepPercent}%</span>
              <span className={statusClass(statusName)}>{statusName}</span>
            </div>
          </div>
          <div className="mt-3 h-1.5 overflow-hidden rounded-full bg-slate-100">
            <div className="h-full rounded-full bg-coris-blue/80 transition-all" style={{ width: `${stepPercent}%` }} />
          </div>
        </div>
        <ChevronDown
          className={`mt-1 h-5 w-5 shrink-0 text-slate-400 transition-transform duration-200 ${expanded ? "rotate-180 text-coris-blue" : ""}`}
        />
      </button>

      {expanded && (
        <div className="border-t border-slate-100 bg-white px-3 py-4 sm:px-4">
          {!editing ? (
            <div className="space-y-4">
              <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
                <DetailField label="Taux d'avancement" value={`${stepPercent}%`} />
                <DetailField label="Statut" value={statusName} />
                <DetailField label="Date de début" value={formatDate(step.dateDebut)} />
                <DetailField label="Date de fin" value={formatDate(step.dateFin)} />
                <DetailField label="Support" value={step.support || "Non renseigné"} className="sm:col-span-2" />
                <DetailField label="Contraintes" value={step.contraintes || "Non renseigné"} className="sm:col-span-2" />
                <DetailField label="Commentaires" value={step.commentaires || "Non renseigné"} className="sm:col-span-2 lg:col-span-3" />
              </div>

              {lastSavedAt && (
                <p className="flex items-center gap-1.5 text-[11px] text-slate-400">
                  <Clock className="h-3.5 w-3.5" />
                  Dernière sauvegarde : {formatDateTime(lastSavedAt)}
                </p>
              )}

              <div className="flex justify-end">
                <Button
                  type="button"
                  variant="outline"
                  onClick={startEditing}
                  className="h-9 rounded-xl border-coris-blue/20 text-xs text-coris-blue hover:bg-coris-blue-soft"
                >
                  <Pencil className="mr-2 h-3.5 w-3.5" />
                  Modifier cette étape
                </Button>
              </div>
            </div>
          ) : (
            <div className="space-y-4">
              <div className="grid gap-3 md:grid-cols-[minmax(0,1fr)_100px_180px]">
                <div className="space-y-1.5">
                  <Label className="text-xs">Libellé *</Label>
                  <Input value={draft.nom} onChange={(event) => updateDraft({ nom: event.target.value })} className="h-10" />
                </div>
                <div className="space-y-1.5">
                  <Label className="text-xs">Taux (%)</Label>
                  <Input
                    type="number"
                    min="0"
                    max="100"
                    value={draft.tauxAvancement}
                    onChange={(event) => updateDraft({ tauxAvancement: event.target.value })}
                    className="h-10"
                  />
                </div>
                <div className="space-y-1.5">
                  <Label className="text-xs">Statut *</Label>
                  <select
                    value={draft.statutEtapeId || ""}
                    onChange={(event) => updateDraft({ statutEtapeId: Number(event.target.value) })}
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
                  <Input type="date" value={draft.dateDebut} onChange={(event) => updateDraft({ dateDebut: event.target.value })} className="h-10" />
                </div>
                <div className="space-y-1.5">
                  <Label className="text-xs">Date de fin</Label>
                  <Input type="date" value={draft.dateFin} onChange={(event) => updateDraft({ dateFin: event.target.value })} className="h-10" />
                </div>
              </div>

              <div className="grid gap-3 sm:grid-cols-2">
                <div className="space-y-1.5">
                  <Label className="text-xs">Support</Label>
                  <Input value={draft.support} onChange={(event) => updateDraft({ support: event.target.value })} placeholder="Support ou fournisseur..." />
                </div>
                <div className="space-y-1.5">
                  <Label className="text-xs">Contraintes</Label>
                  <Input value={draft.contraintes} onChange={(event) => updateDraft({ contraintes: event.target.value })} placeholder="Dépendances, contraintes..." />
                </div>
                <div className="space-y-1.5 sm:col-span-2">
                  <Label className="text-xs">Commentaires</Label>
                  <Textarea
                    value={draft.commentaires}
                    onChange={(event) => updateDraft({ commentaires: event.target.value })}
                    placeholder="Suivi, points d'attention..."
                    className="min-h-20"
                  />
                </div>
              </div>

              {error && <p role="alert" className="text-xs text-red-600">{error}</p>}

              <div className="flex flex-col-reverse gap-2 border-t border-slate-200/80 pt-4 sm:flex-row sm:items-center sm:justify-between">
                {lastSavedAt ? (
                  <p className="flex items-center gap-1.5 text-[11px] text-slate-400">
                    <Clock className="h-3.5 w-3.5" />
                    Dernière sauvegarde : {formatDateTime(lastSavedAt)}
                  </p>
                ) : (
                  <span />
                )}
                <div className="flex flex-col gap-2 sm:flex-row">
                  <Button type="button" variant="outline" onClick={cancelEdit} disabled={saving} className="h-9 rounded-xl text-xs">
                    <X className="mr-2 h-3.5 w-3.5" />
                    Annuler
                  </Button>
                  <Button
                    type="button"
                    onClick={saveStep}
                    disabled={saving}
                    className="h-9 rounded-xl bg-coris-blue text-xs text-white hover:bg-coris-blue/90"
                  >
                    <Save className="mr-2 h-3.5 w-3.5" />
                    {saving ? "Enregistrement..." : "Enregistrer l'étape"}
                  </Button>
                </div>
              </div>
            </div>
          )}
        </div>
      )}
    </article>
  );
}

function DetailField({ label, value, className = "" }: { label: string; value: string; className?: string }) {
  return (
    <div className={className}>
      <p className="text-[10px] font-bold uppercase tracking-[0.12em] text-slate-400">{label}</p>
      <p className="mt-1 whitespace-pre-wrap break-words text-sm text-slate-700">{value}</p>
    </div>
  );
}
