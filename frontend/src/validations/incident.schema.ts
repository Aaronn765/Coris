import { z } from "zod";

export const incidentSchema = z.object({
  intitule: z.string().min(3, "L'intitulé doit faire au moins 3 caractères"),
  description: z.string().min(10, "La description doit faire au moins 10 caractères"),
  dateDeclaration: z.string().min(1, "La date de déclaration est requise"),
  dateFin: z.string().optional(),
  typeIncidentId: z.coerce.number().min(1, "Requis"),
  applicationId: z.coerce.number().min(1, "Requis"),
  entiteId: z.coerce.number().min(1, "Requis"),
  criticiteId: z.coerce.number().min(1, "Requis"),
  impact: z.string().optional(),
  cause: z.string().optional(),
  risqueId: z.coerce.number().min(1, "Requis"),
  actionsMenees: z.string().optional(),
  solution: z.string().optional(),
  actionsEnCours: z.string().optional(),
  responsableN1Id: z.coerce.number().min(1, "Requis"),
  responsableN2Id: z.coerce.number().optional(),
  mesuresPreventives: z.string().optional(),
  statutId: z.coerce.number().min(1, "Requis"),
});

export type IncidentFormValues = z.infer<typeof incidentSchema>;
