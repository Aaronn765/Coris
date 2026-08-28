"use client";

import { useEffect, useState } from "react";
import { Clock, Pencil, Save, X } from "lucide-react";
import { apiService, ProjetWritePayload } from "@/services/api";
import { DomaineProjet, ProjetWithDetails, Responsable, StatutProjet } from "@/types";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { formatDate, formatDateTime } from "@/lib/formatters";
import { buildProjetPayload, toStepDraft } from "@/lib/project-steps";

interface ProjectInfoEditorProps {
  project: ProjetWithDetails;
  onSaved: (project: ProjetWithDetails) => void;
}

export function ProjectInfoEditor({ project, onSaved }: ProjectInfoEditorProps) {
  const [editing, setEditing] = useState(false);
  const [domains, setDomains] = useState<DomaineProjet[]>([]);
  const [statuses, setStatuses] = useState<StatutProjet[]>([]);
  const [responsibles, setResponsibles] = useState<Responsable[]>([]);
  const [loadingRefs, setLoadingRefs] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [name, setName] = useState(project.nom);
  const [domainId, setDomainId] = useState(project.domaineProjetId);
  const [description, setDescription] = useState(project.description ?? "");
  const [rate, setRate] = useState(String(Math.round(project.tauxAvancement * 100)));
  const [dateDebut, setDateDebut] = useState(project.dateDebut?.split("T")[0] ?? "");
  const [dateFin, setDateFin] = useState(project.dateFin?.split("T")[0] ?? "");
  const [dateEcheance, setDateEcheance] = useState(project.dateEcheance?.split("T")[0] ?? "");
  const [statusId, setStatusId] = useState(project.statutProjetId);
  const [support, setSupport] = useState(project.support ?? "");
  const [businessActors, setBusinessActors] = useState(project.acteursMetiers ?? "");
  const [constraints, setConstraints] = useState(project.contraintes ?? "");
  const [comments, setComments] = useState(project.commentaires ?? "");
  const [responsibleIds, setResponsibleIds] = useState<number[]>(project.responsables.map((item) => item.id));

  useEffect(() => {
    if (!editing) return;
    Promise.all([
      apiService.getDomainesProjet(),
      apiService.getStatutsProjet(),
      apiService.getResponsables(),
    ])
      .then(([loadedDomains, loadedStatuses, loadedResponsibles]) => {
        setDomains(loadedDomains);
        setStatuses(loadedStatuses);
        setResponsibles(loadedResponsibles);
      })
      .catch((reason: Error) => setError(reason.message))
      .finally(() => setLoadingRefs(false));
  }, [editing]);

  const startEdit = () => {
    setLoadingRefs(true);
    setEditing(true);
  };

  const resetForm = () => {
    setName(project.nom);
    setDomainId(project.domaineProjetId);
    setDescription(project.description ?? "");
    setRate(String(Math.round(project.tauxAvancement * 100)));
    setDateDebut(project.dateDebut?.split("T")[0] ?? "");
    setDateFin(project.dateFin?.split("T")[0] ?? "");
    setDateEcheance(project.dateEcheance?.split("T")[0] ?? "");
    setStatusId(project.statutProjetId);
    setSupport(project.support ?? "");
    setBusinessActors(project.acteursMetiers ?? "");
    setConstraints(project.contraintes ?? "");
    setComments(project.commentaires ?? "");
    setResponsibleIds(project.responsables.map((item) => item.id));
    setError(null);
  };

  const cancelEdit = () => {
    resetForm();
    setEditing(false);
  };

  const toggleResponsible = (id: number) => {
    setResponsibleIds((current) =>
      current.includes(id) ? current.filter((value) => value !== id) : [...current, id],
    );
  };

  const saveInfo = async () => {
    if (!name.trim() || !domainId || !statusId) {
      setError("Le nom, le domaine et le statut sont obligatoires.");
      return;
    }

    const isoDate = (value: string) => (value ? `${value}T00:00:00.000Z` : undefined);
    const steps = project.etapes.map(toStepDraft);
    const payload: ProjetWritePayload = {
      ...buildProjetPayload(project, steps),
      domaineProjetId: domainId,
      nom: name.trim(),
      description: description.trim() || undefined,
      tauxAvancement: Math.min(100, Math.max(0, Number(rate) || 0)) / 100,
      dateDebut: isoDate(dateDebut),
      dateFin: isoDate(dateFin),
      dateEcheance: isoDate(dateEcheance),
      statutProjetId: statusId,
      support: support.trim() || undefined,
      acteursMetiers: businessActors.trim() || undefined,
      contraintes: constraints.trim() || undefined,
      commentaires: comments.trim() || undefined,
      responsableIds: responsibleIds,
    };

    setSaving(true);
    setError(null);
    try {
      const updated = await apiService.updateProjet(project.id, payload);
      if (!updated) throw new Error("Le projet n'existe plus.");
      onSaved(updated);
      setEditing(false);
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "La mise à jour du projet a échoué.");
    } finally {
      setSaving(false);
    }
  };

  if (!editing) {
    return (
      <section className="rounded-2xl border border-slate-200/80 bg-white p-4 shadow-[0_14px_45px_rgba(13,55,122,0.06)] sm:p-6">
        <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <h2 className="text-xl font-bold text-slate-800">Informations du projet</h2>
            <p className="mt-1 flex items-center gap-1.5 text-xs text-slate-400">
              <Clock className="h-3.5 w-3.5" />
              Dernière modification : {formatDateTime(project.updatedAt)}
            </p>
          </div>
          <Button
            type="button"
            variant="outline"
            onClick={startEdit}
            className="h-9 shrink-0 rounded-xl border-coris-blue/20 text-xs text-coris-blue hover:bg-coris-blue-soft"
          >
            <Pencil className="mr-2 h-3.5 w-3.5" />
            Modifier les informations
          </Button>
        </div>

        <div className="mt-5 grid gap-4 lg:grid-cols-2">
          <InfoBlock title="Description" value={project.description} />
          <InfoBlock title="Responsables" value={project.responsables.map((item) => item.nom).join(" · ") || undefined} />
          <InfoBlock title="Support" value={project.support} />
          <InfoBlock title="Acteurs métiers" value={project.acteursMetiers} />
          <InfoBlock title="Contraintes" value={project.contraintes} />
          <InfoBlock title="Commentaires" value={project.commentaires} />
          <InfoBlock title="Dates" value={[project.dateDebut && `Début : ${formatDate(project.dateDebut)}`, project.dateFin && `Fin : ${formatDate(project.dateFin)}`, project.dateEcheance && `Échéance : ${formatDate(project.dateEcheance)}`].filter(Boolean).join(" · ") || undefined} className="lg:col-span-2" />
        </div>
      </section>
    );
  }

  return (
    <section className="rounded-2xl border border-coris-blue/20 bg-white p-4 shadow-[0_14px_45px_rgba(13,55,122,0.08)] sm:p-6">
      <div className="flex items-start justify-between gap-3">
        <div>
          <h2 className="text-xl font-bold text-slate-800">Modifier les informations</h2>
          <p className="mt-1 text-xs text-slate-400">Les étapes se gèrent dans la section « Suivi du projet ».</p>
        </div>
      </div>

      {loadingRefs ? (
        <div className="mt-6 h-48 animate-pulse rounded-xl bg-white" />
      ) : (
        <div className="mt-5 space-y-5">
          <div className="grid gap-4 md:grid-cols-2">
            <div className="space-y-2 md:col-span-2">
              <Label>Nom du projet *</Label>
              <Input value={name} onChange={(event) => setName(event.target.value)} />
            </div>
            <div className="space-y-2">
              <Label>Domaine *</Label>
              <select
                value={domainId || ""}
                onChange={(event) => setDomainId(Number(event.target.value))}
                className="h-10 w-full rounded-md border border-slate-200 bg-white px-3 text-sm text-slate-700 outline-none focus:border-coris-blue focus:ring-4 focus:ring-coris-blue/10"
              >
                {domains.map((domain) => <option key={domain.id} value={domain.id}>{domain.nom}</option>)}
              </select>
            </div>
            <div className="space-y-2">
              <Label>Statut *</Label>
              <select
                value={statusId || ""}
                onChange={(event) => setStatusId(Number(event.target.value))}
                className="h-10 w-full rounded-md border border-slate-200 bg-white px-3 text-sm text-slate-700 outline-none focus:border-coris-blue focus:ring-4 focus:ring-coris-blue/10"
              >
                {statuses.map((status) => <option key={status.id} value={status.id}>{status.nom}</option>)}
              </select>
            </div>
            <div className="space-y-2 md:col-span-2">
              <Label>Description</Label>
              <Textarea value={description} onChange={(event) => setDescription(event.target.value)} />
            </div>
            <div className="space-y-2">
              <Label>Taux d&apos;avancement global (%)</Label>
              <Input type="number" min="0" max="100" value={rate} onChange={(event) => setRate(event.target.value)} />
            </div>
            <div className="space-y-2">
              <Label>Date d&apos;échéance</Label>
              <Input type="date" value={dateEcheance} onChange={(event) => setDateEcheance(event.target.value)} />
            </div>
            <div className="space-y-2">
              <Label>Date de début</Label>
              <Input type="date" value={dateDebut} onChange={(event) => setDateDebut(event.target.value)} />
            </div>
            <div className="space-y-2">
              <Label>Date de fin</Label>
              <Input type="date" value={dateFin} onChange={(event) => setDateFin(event.target.value)} />
            </div>
          </div>

          <div className="space-y-2 border-t border-slate-100 pt-4">
            <Label>Responsables</Label>
            <div className="grid gap-2 sm:grid-cols-2 lg:grid-cols-3">
              {responsibles.map((responsible) => (
                <label
                  key={responsible.id}
                  className="flex cursor-pointer items-center gap-2 rounded-xl border border-slate-200 px-3 py-2.5 text-sm text-slate-700 transition-colors hover:border-coris-blue/40 hover:bg-coris-blue-soft/30"
                >
                  <input
                    type="checkbox"
                    checked={responsibleIds.includes(responsible.id)}
                    onChange={() => toggleResponsible(responsible.id)}
                    className="h-4 w-4 accent-coris-blue"
                  />
                  {responsible.nom}
                </label>
              ))}
            </div>
          </div>

          <div className="grid gap-4 border-t border-slate-100 pt-4 md:grid-cols-2">
            <div className="space-y-2"><Label>Support</Label><Textarea value={support} onChange={(event) => setSupport(event.target.value)} /></div>
            <div className="space-y-2"><Label>Acteurs métiers</Label><Textarea value={businessActors} onChange={(event) => setBusinessActors(event.target.value)} /></div>
            <div className="space-y-2"><Label>Contraintes</Label><Textarea value={constraints} onChange={(event) => setConstraints(event.target.value)} /></div>
            <div className="space-y-2"><Label>Commentaires</Label><Textarea value={comments} onChange={(event) => setComments(event.target.value)} /></div>
          </div>

          {error && <p role="alert" className="text-sm text-red-600">{error}</p>}

          <div className="flex flex-col-reverse gap-2 border-t border-slate-100 pt-4 sm:flex-row sm:justify-end">
            <Button type="button" variant="outline" onClick={cancelEdit} disabled={saving} className="h-9 rounded-xl text-xs">
              <X className="mr-2 h-3.5 w-3.5" />
              Annuler
            </Button>
            <Button type="button" onClick={saveInfo} disabled={saving} className="h-9 rounded-xl bg-coris-blue text-xs text-white hover:bg-coris-blue/90">
              <Save className="mr-2 h-3.5 w-3.5" />
              {saving ? "Enregistrement..." : "Enregistrer les informations"}
            </Button>
          </div>
        </div>
      )}
    </section>
  );
}

function InfoBlock({ title, value, className = "" }: { title: string; value?: string; className?: string }) {
  return (
    <div className={`rounded-xl border border-slate-100 bg-white p-4 ${className}`}>
      <p className="text-[10px] font-bold uppercase tracking-[0.12em] text-slate-400">{title}</p>
      <p className="mt-2 whitespace-pre-wrap break-words text-sm leading-6 text-slate-600">{value || "Non renseigné"}</p>
    </div>
  );
}
