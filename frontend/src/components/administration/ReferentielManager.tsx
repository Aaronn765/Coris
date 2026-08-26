"use client";

import { useState, useEffect, useCallback } from "react";
import { apiService } from "@/services/api";
import { BaseEntity } from "@/types";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Plus, Edit2, Check, X } from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Label } from "@/components/ui/label";

interface ReferentielManagerProps {
  title: string;
  type: "statuts" | "criticites" | "applications" | "typesIncident" | "entites" | "risques" | "responsables" | "domainesProjet" | "statutsProjet" | "statutsEtape";
}

export function ReferentielManager({ title, type }: ReferentielManagerProps) {
  const [data, setData] = useState<BaseEntity[]>([]);
  const [loading, setLoading] = useState(true);
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [editingItem, setEditingItem] = useState<BaseEntity | null>(null);
  const [nom, setNom] = useState("");

  const fetchData = useCallback(async () => {
    setLoading(true);
    let result: BaseEntity[] = [];
    switch (type) {
      case "statuts": result = await apiService.getStatuts(true); break;
      case "criticites": result = await apiService.getCriticites(true); break;
      case "applications": result = await apiService.getApplications(true); break;
      case "typesIncident": result = await apiService.getTypesIncident(true); break;
      case "entites": result = await apiService.getEntites(true); break;
      case "risques": result = await apiService.getRisques(true); break;
      case "responsables": result = await apiService.getResponsables(true); break;
      case "domainesProjet": result = await apiService.getDomainesProjet(true); break;
      case "statutsProjet": result = await apiService.getStatutsProjet(true); break;
      case "statutsEtape": result = await apiService.getStatutsEtape(true); break;
    }
    setData(result);
    setLoading(false);
  }, [type]);

  useEffect(() => {
    let disposed = false;
    const timer = window.setTimeout(() => {
      if (!disposed) void fetchData();
    }, 0);
    return () => {
      disposed = true;
      window.clearTimeout(timer);
    };
  }, [fetchData]);

  const handleSave = async () => {
    if (!nom.trim()) return;
    
    if (editingItem) {
      // call update
      await apiService.updateReferentiel(type, editingItem.id, { nom, actif: editingItem.actif });
    } else {
      // call create
      await apiService.createReferentiel(type, { nom, actif: true });
    }
    
    setIsDialogOpen(false);
    setEditingItem(null);
    setNom("");
    fetchData(); // reload
  };

  const handleToggleActif = async (item: BaseEntity) => {
    await apiService.updateReferentiel(type, item.id, { nom: item.nom, actif: !item.actif });
    fetchData();
  };

  const openEdit = (item: BaseEntity) => {
    setEditingItem(item);
    setNom(item.nom);
    setIsDialogOpen(true);
  };

  const openCreate = () => {
    setEditingItem(null);
    setNom("");
    setIsDialogOpen(true);
  };

  if (loading) return <div className="p-4">Chargement {title}...</div>;

  return (
    <div className="space-y-4">
      <div className="flex justify-between items-center">
        <h3 className="text-lg font-medium">{title}</h3>
        <Button size="sm" onClick={openCreate}><Plus className="h-4 w-4 mr-2" /> Ajouter</Button>
        <Dialog open={isDialogOpen} onOpenChange={setIsDialogOpen}>
          <DialogContent>
            <DialogHeader>
              <DialogTitle>{editingItem ? "Modifier" : "Ajouter"} - {title}</DialogTitle>
            </DialogHeader>
            <div className="space-y-4 py-4">
              <div className="space-y-2">
                <Label>Nom</Label>
                <Input value={nom} onChange={(e) => setNom(e.target.value)} placeholder="Nom de la valeur" />
              </div>
              <div className="flex justify-end gap-2">
                <Button variant="outline" onClick={() => setIsDialogOpen(false)}>Annuler</Button>
                <Button onClick={handleSave}>Enregistrer</Button>
              </div>
            </div>
          </DialogContent>
        </Dialog>
      </div>

      <div className="border rounded-md bg-white">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Nom</TableHead>
              <TableHead className="w-24 text-center">Statut</TableHead>
              <TableHead className="w-24 text-right">Actions</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {data.map((item) => (
              <TableRow key={item.id}>
                <TableCell className="font-medium">{item.nom}</TableCell>
                <TableCell className="text-center">
                  <span className={`inline-flex items-center px-2 py-1 rounded-full text-xs font-semibold ${item.actif ? 'bg-success/20 text-success' : 'bg-muted text-muted-foreground'}`}>
                    {item.actif ? "Actif" : "Inactif"}
                  </span>
                </TableCell>
                <TableCell className="text-right">
                  <div className="flex items-center justify-end gap-2">
                    <Button variant="ghost" size="icon" onClick={() => openEdit(item)}>
                      <Edit2 className="h-4 w-4" />
                    </Button>
                    <Button variant="ghost" size="icon" onClick={() => handleToggleActif(item)}>
                      {item.actif ? <X className="h-4 w-4 text-danger" /> : <Check className="h-4 w-4 text-success" />}
                    </Button>
                  </div>
                </TableCell>
              </TableRow>
            ))}
            {data.length === 0 && (
              <TableRow>
                <TableCell colSpan={3} className="text-center py-4">Aucune donnée.</TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>
    </div>
  );
}
