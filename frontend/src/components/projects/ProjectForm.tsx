"use client";

import { useEffect, useState } from "react";
import { Plus, Save, Trash2 } from "lucide-react";
import { useRouter } from "next/navigation";
import { apiService, ProjetWritePayload } from "@/services/api";
import { DomaineProjet, ProjetWithDetails, Responsable, StatutEtape, StatutProjet } from "@/types";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";

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

const clean = (value: string) => value.normalize("NFD").replace(/[\u0300-\u036f]/g, "").toLowerCase();
const isoDate = (value: string) => value ? `${value}T00:00:00.000Z` : undefined;
const percentValue = (value: number) => String(Math.round(value * 100));

function newStep(statuses: StatutEtape[]): StepDraft {
  const defaultStatus = statuses.find((status) => clean(status.nom).includes("non") || clean(status.nom).includes("plan"))?.id ?? statuses[0]?.id ?? 0;
  return { nom: "", tauxAvancement: "0", statutEtapeId: defaultStatus, dateDebut: "", dateFin: "", support: "", contraintes: "", commentaires: "" };
}

interface ProjectFormProps {
  initialData?: ProjetWithDetails;
}

export function ProjectForm({ initialData }: ProjectFormProps) {
  const router = useRouter();
  const [domains, setDomains] = useState<DomaineProjet[]>([]);
  const [projectStatuses, setProjectStatuses] = useState<StatutProjet[]>([]);
  const [stepStatuses, setStepStatuses] = useState<StatutEtape[]>([]);
  const [responsibles, setResponsibles] = useState<Responsable[]>([]);
  const [name, setName] = useState(initialData?.nom ?? "");
  const [domainId, setDomainId] = useState(initialData?.domaineProjetId ?? 0);
  const [description, setDescription] = useState(initialData?.description ?? "");
  const [rate, setRate] = useState(percentValue(initialData?.tauxAvancement ?? 0));
  const [dateDebut, setDateDebut] = useState(initialData?.dateDebut?.split("T")[0] ?? "");
  const [dateFin, setDateFin] = useState(initialData?.dateFin?.split("T")[0] ?? "");
  const [dateEcheance, setDateEcheance] = useState(initialData?.dateEcheance?.split("T")[0] ?? "");
  const [statusId, setStatusId] = useState(initialData?.statutProjetId ?? 1);
  const [support, setSupport] = useState(initialData?.support ?? "");
  const [businessActors, setBusinessActors] = useState(initialData?.acteursMetiers ?? "");
  const [constraints, setConstraints] = useState(initialData?.contraintes ?? "");
  const [comments, setComments] = useState(initialData?.commentaires ?? "");
  const [responsibleIds, setResponsibleIds] = useState<number[]>(initialData?.responsables.map((item) => item.id) ?? []);
  const [steps, setSteps] = useState<StepDraft[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    Promise.all([
      apiService.getDomainesProjet(),
      apiService.getStatutsProjet(),
      apiService.getStatutsEtape(),
      apiService.getResponsables(),
    ]).then(([loadedDomains, loadedProjectStatuses, loadedStepStatuses, loadedResponsibles]) => {
      setDomains(loadedDomains);
      setProjectStatuses(loadedProjectStatuses);
      setStepStatuses(loadedStepStatuses);
      setResponsibles(loadedResponsibles);
      if (!initialData) {
        setDomainId(loadedDomains[0]?.id ?? 0);
        setStatusId(loadedProjectStatuses.find((status) => clean(status.nom).includes("planifie"))?.id ?? loadedProjectStatuses[0]?.id ?? 0);
      }
      if (initialData) {
        setSteps(initialData.etapes.map((step) => ({
          nom: step.nom,
          tauxAvancement: percentValue(step.tauxAvancement),
          statutEtapeId: step.statutEtapeId,
          dateDebut: step.dateDebut?.split("T")[0] ?? "",
          dateFin: step.dateFin?.split("T")[0] ?? "",
          support: step.support ?? "",
          contraintes: step.contraintes ?? "",
          commentaires: step.commentaires ?? "",
        })));
      }
    }).catch((reason: Error) => setError(reason.message)).finally(() => setLoading(false));
  }, [initialData]);

  const toggleResponsible = (id: number) => {
    setResponsibleIds((current) => current.includes(id) ? current.filter((value) => value !== id) : [...current, id]);
  };

  const updateStep = (index: number, changes: Partial<StepDraft>) => {
    setSteps((current) => current.map((step, stepIndex) => stepIndex === index ? { ...step, ...changes } : step));
  };

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!name.trim() || !domainId || !statusId) {
      setError("Le nom, le domaine et le statut du projet sont obligatoires.");
      return;
    }
    if (steps.some((step) => !step.nom.trim() || !step.statutEtapeId)) {
      setError("Chaque étape doit avoir un nom et un statut.");
      return;
    }

    const payload: ProjetWritePayload = {
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
      etapes: steps.map((step, index) => ({
        nom: step.nom.trim(),
        tauxAvancement: Math.min(100, Math.max(0, Number(step.tauxAvancement) || 0)) / 100,
        statutEtapeId: step.statutEtapeId,
        dateDebut: isoDate(step.dateDebut),
        dateFin: isoDate(step.dateFin),
        support: step.support.trim() || undefined,
        contraintes: step.contraintes.trim() || undefined,
        commentaires: step.commentaires.trim() || undefined,
        ordre: index,
      })),
    };

    setSaving(true);
    setError(null);
    try {
      const saved = initialData
        ? await apiService.updateProjet(initialData.id, payload)
        : await apiService.createProjet(payload);
      if (saved) router.push(`/projets/${saved.id}`);
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "L'enregistrement du projet a échoué.");
    } finally {
      setSaving(false);
    }
  };

  if (loading) return <div className="h-96 animate-pulse rounded-2xl bg-white shadow-sm" />;

  return (
    <form onSubmit={handleSubmit} className="space-y-6 rounded-2xl border border-slate-200/80 bg-white p-4 shadow-[0_14px_45px_rgba(13,55,122,0.06)] sm:p-6">
      {error && <div role="alert" className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">{error}</div>}

      <section className="space-y-4">
        <div><h2 className="text-lg font-bold text-slate-800">Informations générales</h2><p className="mt-1 text-xs text-slate-400">Le numéro PRJ est généré automatiquement à la création.</p></div>
        <div className="grid gap-4 md:grid-cols-2">
          <div className="space-y-2 md:col-span-2"><Label>Nom du projet *</Label><Input value={name} onChange={(event) => setName(event.target.value)} placeholder="Ex. Projet SD-WAN pour les agences" /></div>
          <div className="space-y-2"><Label>Domaine *</Label><select value={domainId || ""} onChange={(event) => setDomainId(Number(event.target.value))} className="h-10 w-full rounded-md border border-slate-200 bg-white px-3 text-sm text-slate-700 outline-none focus:border-coris-blue focus:ring-4 focus:ring-coris-blue/10"><option value="">Sélectionner un domaine</option>{domains.map((domain) => <option key={domain.id} value={domain.id}>{domain.nom}</option>)}</select></div>
          <div className="space-y-2"><Label>Statut *</Label><select value={statusId || ""} onChange={(event) => setStatusId(Number(event.target.value))} className="h-10 w-full rounded-md border border-slate-200 bg-white px-3 text-sm text-slate-700 outline-none focus:border-coris-blue focus:ring-4 focus:ring-coris-blue/10"><option value="">Sélectionner un statut</option>{projectStatuses.map((status) => <option key={status.id} value={status.id}>{status.nom}</option>)}</select></div>
          <div className="space-y-2 md:col-span-2"><Label>Description du projet</Label><Textarea value={description} onChange={(event) => setDescription(event.target.value)} placeholder="Objectif, périmètre et résultat attendu..." /></div>
          <div className="space-y-2"><Label>Taux d&apos;avancement (%)</Label><Input type="number" min="0" max="100" value={rate} onChange={(event) => setRate(event.target.value)} /></div>
          <div className="space-y-2"><Label>Date d&apos;échéance</Label><Input type="date" value={dateEcheance} onChange={(event) => setDateEcheance(event.target.value)} /></div>
          <div className="space-y-2"><Label>Date de début</Label><Input type="date" value={dateDebut} onChange={(event) => setDateDebut(event.target.value)} /></div>
          <div className="space-y-2"><Label>Date de fin</Label><Input type="date" value={dateFin} onChange={(event) => setDateFin(event.target.value)} /></div>
        </div>
      </section>

      <section className="space-y-3 border-t border-slate-100 pt-5">
        <div className="flex items-center justify-between gap-3"><div><h2 className="text-lg font-bold text-slate-800">Responsables</h2><p className="mt-1 text-xs text-slate-400">Un projet peut avoir plusieurs responsables.</p></div></div>
        <div className="grid gap-2 sm:grid-cols-2 lg:grid-cols-3">{responsibles.map((responsible) => <label key={responsible.id} className="flex cursor-pointer items-center gap-2 rounded-xl border border-slate-200 px-3 py-2.5 text-sm text-slate-700 transition-colors hover:border-coris-blue/40 hover:bg-coris-blue-soft/30"><input type="checkbox" checked={responsibleIds.includes(responsible.id)} onChange={() => toggleResponsible(responsible.id)} className="h-4 w-4 accent-coris-blue" />{responsible.nom}</label>)}</div>
      </section>

      <section className="space-y-4 border-t border-slate-100 pt-5">
        <div className="flex flex-col justify-between gap-3 sm:flex-row sm:items-center"><div><h2 className="text-lg font-bold text-slate-800">Étapes du projet</h2><p className="mt-1 text-xs text-slate-400">Chaque étape conserve son taux, son statut et ses dates.</p></div><Button type="button" variant="outline" onClick={() => setSteps((current) => [...current, newStep(stepStatuses)])} className="rounded-xl border-coris-blue/20 text-coris-blue hover:bg-coris-blue-soft"><Plus className="mr-2 h-4 w-4" /> Ajouter une étape</Button></div>
        {steps.length === 0 && <div className="rounded-xl border border-dashed border-slate-200 px-4 py-8 text-center text-sm text-slate-400">Aucune étape ajoutée pour le moment.</div>}
        <div className="space-y-3">{steps.map((step, index) => <div key={`${index}-${step.nom}`} className="rounded-xl border border-slate-200 bg-slate-50/60 p-4"><div className="grid gap-3 md:grid-cols-[minmax(0,1fr)_120px_180px_42px]"><div className="space-y-2"><Label>Étape {index + 1}</Label><Input value={step.nom} onChange={(event) => updateStep(index, { nom: event.target.value })} placeholder="Ex. Installation des équipements" /></div><div className="space-y-2"><Label>Taux (%)</Label><Input type="number" min="0" max="100" value={step.tauxAvancement} onChange={(event) => updateStep(index, { tauxAvancement: event.target.value })} /></div><div className="space-y-2"><Label>Statut</Label><select value={step.statutEtapeId || ""} onChange={(event) => updateStep(index, { statutEtapeId: Number(event.target.value) })} className="h-10 w-full rounded-md border border-slate-200 bg-white px-3 text-sm text-slate-700 outline-none focus:border-coris-blue focus:ring-4 focus:ring-coris-blue/10">{stepStatuses.map((status) => <option key={status.id} value={status.id}>{status.nom}</option>)}</select></div><div className="flex items-end justify-end"><Button type="button" variant="ghost" size="icon" onClick={() => setSteps((current) => current.filter((_, stepIndex) => stepIndex !== index))} aria-label={`Supprimer l'étape ${index + 1}`} className="text-slate-400 hover:bg-red-50 hover:text-coris-red"><Trash2 className="h-4 w-4" /></Button></div></div><div className="mt-3 grid gap-3 sm:grid-cols-2"><div className="space-y-2"><Label>Date de début</Label><Input type="date" value={step.dateDebut} onChange={(event) => updateStep(index, { dateDebut: event.target.value })} /></div><div className="space-y-2"><Label>Date de fin</Label><Input type="date" value={step.dateFin} onChange={(event) => updateStep(index, { dateFin: event.target.value })} /></div></div><details className="mt-3"><summary className="cursor-pointer text-xs font-bold text-coris-blue">Informations complémentaires de l&apos;étape</summary><div className="mt-3 grid gap-3 sm:grid-cols-2"><div className="space-y-2"><Label>Support</Label><Input value={step.support} onChange={(event) => updateStep(index, { support: event.target.value })} /></div><div className="space-y-2"><Label>Contraintes</Label><Input value={step.contraintes} onChange={(event) => updateStep(index, { contraintes: event.target.value })} /></div><div className="space-y-2 sm:col-span-2"><Label>Commentaires</Label><Textarea value={step.commentaires} onChange={(event) => updateStep(index, { commentaires: event.target.value })} /></div></div></details></div>)}</div>
      </section>

      <section className="grid gap-4 border-t border-slate-100 pt-5 md:grid-cols-3">
        <div className="space-y-2"><Label>Support</Label><Textarea value={support} onChange={(event) => setSupport(event.target.value)} placeholder="Support ou fournisseur..." /></div>
        <div className="space-y-2"><Label>Acteurs métiers</Label><Textarea value={businessActors} onChange={(event) => setBusinessActors(event.target.value)} placeholder="Directions, métiers, partenaires..." /></div>
        <div className="space-y-2"><Label>Contraintes</Label><Textarea value={constraints} onChange={(event) => setConstraints(event.target.value)} placeholder="Dépendances ou contraintes opérationnelles..." /></div>
        <div className="space-y-2 md:col-span-3"><Label>Commentaires</Label><Textarea value={comments} onChange={(event) => setComments(event.target.value)} placeholder="Suivi, points d'attention, remarques..." /></div>
      </section>

      <div className="flex flex-col-reverse gap-3 border-t border-slate-100 pt-5 sm:flex-row sm:justify-end"><Button type="button" variant="outline" onClick={() => router.back()} disabled={saving} className="rounded-xl">Annuler</Button><Button type="submit" disabled={saving} className="rounded-xl bg-coris-red text-white hover:bg-[#c91428]"><Save className="mr-2 h-4 w-4" />{saving ? "Enregistrement..." : initialData ? "Enregistrer les modifications" : "Créer le projet"}</Button></div>
    </form>
  );
}
