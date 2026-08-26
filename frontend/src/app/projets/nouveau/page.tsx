import { ProjectForm } from "@/components/projects/ProjectForm";

export default function NouveauProjetPage() {
  return <div className="animate-fade-up mx-auto max-w-6xl space-y-4 sm:space-y-6"><div><h1 className="text-2xl font-bold tracking-tight text-slate-800 sm:text-3xl">Nouveau projet</h1><p className="mt-1 text-sm text-slate-500">Ajoutez le projet, ses responsables et ses étapes de suivi.</p></div><ProjectForm /></div>;
}
