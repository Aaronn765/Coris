"use client";

import { ReferentielManager } from "@/components/administration/ReferentielManager";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";

export default function AdministrationPage() {
  return (
    <div className="animate-fade-up space-y-4 sm:space-y-6">
      <div><h1 className="text-3xl font-bold tracking-tight text-slate-800">Paramètres</h1></div>
      <Tabs defaultValue="statuts" className="w-full">
        <TabsList className="flex h-auto max-w-full flex-wrap gap-1 rounded-xl border border-slate-200 bg-white p-1.5 shadow-sm sm:rounded-2xl">
          <TabsTrigger value="statuts">Statuts</TabsTrigger>
          <TabsTrigger value="applications">Applications</TabsTrigger>
          <TabsTrigger value="typesIncident">Types d&apos;incident</TabsTrigger>
          <TabsTrigger value="entites">Entités</TabsTrigger>
          <TabsTrigger value="criticites">Criticités</TabsTrigger>
          <TabsTrigger value="risques">Risques</TabsTrigger>
          <TabsTrigger value="responsables">Responsables</TabsTrigger>
          <TabsTrigger value="domainesProjet">Domaines projets</TabsTrigger>
          <TabsTrigger value="statutsProjet">Statuts projets</TabsTrigger>
          <TabsTrigger value="statutsEtape">Statuts étapes</TabsTrigger>
        </TabsList>
        <div className="mt-4 sm:mt-6">
          <TabsContent value="statuts"><ReferentielManager title="Statuts des incidents" type="statuts" /></TabsContent>
          <TabsContent value="applications"><ReferentielManager title="Applications" type="applications" /></TabsContent>
          <TabsContent value="typesIncident"><ReferentielManager title="Types d&apos;incident" type="typesIncident" /></TabsContent>
          <TabsContent value="entites"><ReferentielManager title="Entites / Perimetres" type="entites" /></TabsContent>
          <TabsContent value="criticites"><ReferentielManager title="Criticites" type="criticites" /></TabsContent>
          <TabsContent value="risques"><ReferentielManager title="Risques pour la banque" type="risques" /></TabsContent>
          <TabsContent value="responsables"><ReferentielManager title="Responsables" type="responsables" /></TabsContent>
          <TabsContent value="domainesProjet"><ReferentielManager title="Domaines des projets" type="domainesProjet" /></TabsContent>
          <TabsContent value="statutsProjet"><ReferentielManager title="Statuts des projets" type="statutsProjet" /></TabsContent>
          <TabsContent value="statutsEtape"><ReferentielManager title="Statuts des étapes" type="statutsEtape" /></TabsContent>
        </div>
      </Tabs>
    </div>
  );
}
