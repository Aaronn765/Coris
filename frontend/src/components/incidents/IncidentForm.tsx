"use client";

/* The existing form relies on React Hook Form's dynamic field values. */
/* eslint-disable @typescript-eslint/no-explicit-any, react/no-unescaped-entities */

import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { incidentSchema, IncidentFormValues } from "@/validations/incident.schema";
import { apiService } from "@/services/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
} from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import { Label } from "@/components/ui/label";
import { Application, Criticite, Entite, Responsable, Risque, Statut, TypeIncident, IncidentWithDetails } from "@/types";
import { useRouter } from "next/navigation";

interface IncidentFormProps {
  initialData?: IncidentWithDetails;
}

export function IncidentForm({ initialData }: IncidentFormProps) {
  const router = useRouter();
  
  // Referentials state
  const [applications, setApplications] = useState<Application[]>([]);
  const [types, setTypes] = useState<TypeIncident[]>([]);
  const [entites, setEntites] = useState<Entite[]>([]);
  const [criticites, setCriticites] = useState<Criticite[]>([]);
  const [risques, setRisques] = useState<Risque[]>([]);
  const [statuts, setStatuts] = useState<Statut[]>([]);
  const [responsables, setResponsables] = useState<Responsable[]>([]);
  
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    Promise.all([
      apiService.getApplications(),
      apiService.getTypesIncident(),
      apiService.getEntites(),
      apiService.getCriticites(),
      apiService.getRisques(),
      apiService.getStatuts(),
      apiService.getResponsables()
    ]).then(([apps, typs, ents, crits, risq, stats, resp]) => {
      setApplications(apps);
      setTypes(typs);
      setEntites(ents);
      setCriticites(crits);
      setRisques(risq);
      setStatuts(stats);
      setResponsables(resp);
      setLoading(false);
    });
  }, []);

  const form = useForm<IncidentFormValues>({
    resolver: zodResolver(incidentSchema) as any,
    defaultValues: initialData ? {
      intitule: initialData.intitule,
      description: initialData.description,
      dateDeclaration: initialData.dateDeclaration.split('T')[0],
      dateFin: initialData.dateFin ? initialData.dateFin.split('T')[0] : "",
      typeIncidentId: initialData.typeIncidentId,
      applicationId: initialData.applicationId,
      entiteId: initialData.entiteId,
      criticiteId: initialData.criticiteId,
      risqueId: initialData.risqueId,
      statutId: initialData.statutId,
      responsableN1Id: initialData.responsableN1Id,
      responsableN2Id: initialData.responsableN2Id,
      impact: initialData.impact || "",
      cause: initialData.cause || "",
      actionsMenees: initialData.actionsMenees || "",
      solution: initialData.solution || "",
      actionsEnCours: initialData.actionsEnCours || "",
      mesuresPreventives: initialData.mesuresPreventives || "",
    } : {
      intitule: "",
      description: "",
      dateDeclaration: new Date().toISOString().split('T')[0],
      dateFin: "",
      typeIncidentId: 0,
      applicationId: 0,
      entiteId: 0,
      criticiteId: 0,
      risqueId: 0,
      statutId: 1, // Nouveau by default
      responsableN1Id: 0,
      responsableN2Id: 0,
      impact: "",
      cause: "",
      actionsMenees: "",
      solution: "",
      actionsEnCours: "",
      mesuresPreventives: "",
    },
  });

  async function onSubmit(data: IncidentFormValues) {
    setSubmitting(true);
    try {
      // Nettoyage des champs optionnels valant 0
      const payload: any = { ...data };
      if (payload.responsableN2Id === 0) delete payload.responsableN2Id;
      if (!payload.dateFin) delete payload.dateFin;
      
      // Convert to ISO if it's a date string
      if (payload.dateDeclaration) payload.dateDeclaration = new Date(payload.dateDeclaration).toISOString();
      if (payload.dateFin) payload.dateFin = new Date(payload.dateFin).toISOString();

      if (initialData) {
        await apiService.updateIncident(initialData.id, payload);
      } else {
        await apiService.createIncident(payload as any);
      }
      router.push("/incidents");
    } catch (error) {
      console.error(error);
    } finally {
      setSubmitting(false);
    }
  }

  if (loading) return <div>Chargement des référentiels...</div>;

  const { register, handleSubmit, formState: { errors }, setValue, watch } = form;

  const getNom = (list: any[], id: any) => {
    if (!id || id === 0) return "Sélectionnez";
    return list.find(item => item.id === Number(id))?.nom || "Sélectionnez";
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-6 rounded-xl border bg-white p-4 shadow-sm sm:space-y-8 sm:p-6">
      <div className="grid grid-cols-1 gap-4 md:grid-cols-2 md:gap-6">
        <div className="space-y-2 md:col-span-2">
          <Label>Intitulé de l'incident *</Label>
          <Input placeholder="Ex: Impossible de se connecter..." {...register("intitule")} />
          {errors.intitule && <p className="text-sm text-danger">{errors.intitule.message}</p>}
        </div>

        <div className="space-y-2 md:col-span-2">
          <Label>Description détaillée *</Label>
          <Textarea className="h-32" placeholder="Décrivez l'incident..." {...register("description")} />
          {errors.description && <p className="text-sm text-danger">{errors.description.message}</p>}
        </div>

        <div className="space-y-2">
          <Label>Date de déclaration *</Label>
          <Input type="date" {...register("dateDeclaration")} />
          {errors.dateDeclaration && <p className="text-sm text-danger">{errors.dateDeclaration.message}</p>}
        </div>

        <div className="space-y-2">
          <Label>Date de fin</Label>
          <Input type="date" {...register("dateFin")} />
          {errors.dateFin && <p className="text-sm text-danger">{errors.dateFin.message}</p>}
        </div>

        <div className="space-y-2">
          <Label>Application *</Label>
          <Select onValueChange={(val) => setValue("applicationId", Number(val))} value={watch("applicationId")?.toString()}>
            <SelectTrigger>
              <span className={!watch("applicationId") ? "text-muted-foreground" : "truncate"}>
                {getNom(applications, watch("applicationId"))}
              </span>
            </SelectTrigger>
            <SelectContent>
              {applications.filter(a => a.actif).map(app => (
                <SelectItem key={app.id} value={app.id.toString()}>{app.nom}</SelectItem>
              ))}
            </SelectContent>
          </Select>
          {errors.applicationId && <p className="text-sm text-danger">{errors.applicationId.message}</p>}
        </div>

        <div className="space-y-2">
          <Label>Type d'incident *</Label>
          <Select onValueChange={(val) => setValue("typeIncidentId", Number(val))} value={watch("typeIncidentId")?.toString()}>
            <SelectTrigger>
              <span className={!watch("typeIncidentId") ? "text-muted-foreground" : "truncate"}>
                {getNom(types, watch("typeIncidentId"))}
              </span>
            </SelectTrigger>
            <SelectContent>
              {types.filter(t => t.actif).map(t => (
                <SelectItem key={t.id} value={t.id.toString()}>{t.nom}</SelectItem>
              ))}
            </SelectContent>
          </Select>
          {errors.typeIncidentId && <p className="text-sm text-danger">{errors.typeIncidentId.message}</p>}
        </div>
        
        <div className="space-y-2">
          <Label>Entité / Périmètre *</Label>
          <Select onValueChange={(val) => setValue("entiteId", Number(val))} value={watch("entiteId")?.toString()}>
            <SelectTrigger>
              <span className={!watch("entiteId") ? "text-muted-foreground" : "truncate"}>
                {getNom(entites, watch("entiteId"))}
              </span>
            </SelectTrigger>
            <SelectContent>
              {entites.filter(e => e.actif).map(e => (
                <SelectItem key={e.id} value={e.id.toString()}>{e.nom}</SelectItem>
              ))}
            </SelectContent>
          </Select>
          {errors.entiteId && <p className="text-sm text-danger">{errors.entiteId.message}</p>}
        </div>

        <div className="space-y-2">
          <Label>Criticité *</Label>
          <Select onValueChange={(val) => setValue("criticiteId", Number(val))} value={watch("criticiteId")?.toString()}>
            <SelectTrigger>
              <span className={!watch("criticiteId") ? "text-muted-foreground" : "truncate"}>
                {getNom(criticites, watch("criticiteId"))}
              </span>
            </SelectTrigger>
            <SelectContent>
              {criticites.filter(c => c.actif).map(c => (
                <SelectItem key={c.id} value={c.id.toString()}>{c.nom}</SelectItem>
              ))}
            </SelectContent>
          </Select>
          {errors.criticiteId && <p className="text-sm text-danger">{errors.criticiteId.message}</p>}
        </div>

        <div className="space-y-2">
          <Label>Risque pour la banque *</Label>
          <Select onValueChange={(val) => setValue("risqueId", Number(val))} value={watch("risqueId")?.toString()}>
            <SelectTrigger>
              <span className={!watch("risqueId") ? "text-muted-foreground" : "truncate"}>
                {getNom(risques, watch("risqueId"))}
              </span>
            </SelectTrigger>
            <SelectContent>
              {risques.filter(r => r.actif).map(r => (
                <SelectItem key={r.id} value={r.id.toString()}>{r.nom}</SelectItem>
              ))}
            </SelectContent>
          </Select>
          {errors.risqueId && <p className="text-sm text-danger">{errors.risqueId.message}</p>}
        </div>

        <div className="space-y-2">
          <Label>Statut *</Label>
          <Select onValueChange={(val) => setValue("statutId", Number(val))} value={watch("statutId")?.toString()}>
            <SelectTrigger>
              <span className={!watch("statutId") ? "text-muted-foreground" : "truncate"}>
                {getNom(statuts, watch("statutId"))}
              </span>
            </SelectTrigger>
            <SelectContent>
              {statuts.filter(s => s.actif).map(s => (
                <SelectItem key={s.id} value={s.id.toString()}>{s.nom}</SelectItem>
              ))}
            </SelectContent>
          </Select>
          {errors.statutId && <p className="text-sm text-danger">{errors.statutId.message}</p>}
        </div>
        
        <div className="space-y-2">
          <Label>Responsable N1 *</Label>
          <Select onValueChange={(val) => setValue("responsableN1Id", Number(val))} value={watch("responsableN1Id")?.toString()}>
            <SelectTrigger>
              <span className={!watch("responsableN1Id") ? "text-muted-foreground" : "truncate"}>
                {getNom(responsables, watch("responsableN1Id"))}
              </span>
            </SelectTrigger>
            <SelectContent>
              {responsables.filter(r => r.actif).map(r => (
                <SelectItem key={r.id} value={r.id.toString()}>{r.nom}</SelectItem>
              ))}
            </SelectContent>
          </Select>
          {errors.responsableN1Id && <p className="text-sm text-danger">{errors.responsableN1Id.message}</p>}
        </div>

        <div className="space-y-2">
          <Label>Responsable N2 (optionnel)</Label>
          <Select onValueChange={(val) => setValue("responsableN2Id", Number(val))} value={watch("responsableN2Id")?.toString()}>
            <SelectTrigger>
              <span className={!watch("responsableN2Id") ? "text-muted-foreground" : "truncate"}>
                {getNom(responsables, watch("responsableN2Id"))}
              </span>
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="0">Aucun</SelectItem>
              {responsables.filter(r => r.actif).map(r => (
                <SelectItem key={r.id} value={r.id.toString()}>{r.nom}</SelectItem>
              ))}
            </SelectContent>
          </Select>
          {errors.responsableN2Id && <p className="text-sm text-danger">{errors.responsableN2Id.message}</p>}
        </div>
      </div>
      
      <div className="space-y-4 sm:space-y-6">
        <h3 className="text-lg font-medium border-b pb-2">Informations complémentaires</h3>
        
        <div className="space-y-2">
          <Label>Impact</Label>
          <Textarea placeholder="Impact sur les utilisateurs/métiers..." {...register("impact")} />
        </div>

        <div className="space-y-2">
          <Label>Cause racine</Label>
          <Textarea placeholder="Pourquoi l'incident s'est-il produit ?" {...register("cause")} />
        </div>

        <div className="space-y-2">
          <Label>Actions menées</Label>
          <Textarea placeholder="Ce qui a été fait..." {...register("actionsMenees")} />
        </div>

        <div className="space-y-2">
          <Label>Solution apportée</Label>
          <Textarea placeholder="Comment l'incident a-t-il été résolu ?" {...register("solution")} />
        </div>

        <div className="space-y-2">
          <Label>Actions en cours</Label>
          <Textarea placeholder="Actions restantes à finaliser..." {...register("actionsEnCours")} />
        </div>

        <div className="space-y-2">
          <Label>Action & mesures pour éviter que l'incident se reproduise</Label>
          <Textarea placeholder="Mesures préventives..." {...register("mesuresPreventives")} />
        </div>
      </div>

      <div className="grid gap-3 border-t pt-4 sm:flex sm:justify-end sm:gap-4">
        <Button type="button" variant="outline" onClick={() => router.push("/incidents")} disabled={submitting} className="w-full sm:w-auto">
          Annuler
        </Button>
        <Button type="submit" disabled={submitting} className="w-full sm:w-auto">
          {submitting ? "Enregistrement..." : (initialData ? "Mettre à jour" : "Créer l'incident")}
        </Button>
      </div>
    </form>
  );
}
