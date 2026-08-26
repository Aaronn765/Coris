import { IncidentForm } from "@/components/incidents/IncidentForm";

export default function NouveauIncidentPage() {
  return (
    <div className="animate-fade-up mx-auto max-w-4xl space-y-4 sm:space-y-6">
      <div><h1 className="text-2xl font-bold tracking-tight text-slate-800 sm:text-3xl">Nouvel incident</h1></div>
      <IncidentForm />
    </div>
  );
}
