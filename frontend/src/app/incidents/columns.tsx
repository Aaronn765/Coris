"use client";

import { ColumnDef } from "@tanstack/react-table";
import { ArrowUpRight, Clock3 } from "lucide-react";
import Link from "next/link";
import { IncidentWithDetails } from "@/types";
import { formatDate, formatDuration } from "@/lib/formatters";

type ColumnMeta = { className?: string };

const normalized = (value: string) =>
  value.normalize("NFD").replace(/[\u0300-\u036f]/g, "").toLowerCase();

function criticiteClass(value: string) {
  const name = normalized(value);
  if (["critique", "fort", "haute"].includes(name)) return "coris-badge coris-badge-danger";
  if (["moyenne", "moderee"].includes(name)) return "coris-badge coris-badge-warning";
  return "coris-badge coris-badge-info";
}

function statutClass(value: string) {
  const name = normalized(value);
  return ["resolu", "cloture", "ferme"].includes(name)
    ? "coris-badge coris-badge-success"
    : "coris-badge coris-badge-warning";
}

export const columns: ColumnDef<IncidentWithDetails>[] = [
  {
    accessorKey: "numero",
    header: "N°",
    meta: { className: "w-[132px]" } satisfies ColumnMeta,
    cell: ({ row }) => (
      <Link href={`/incidents/${row.original.id}`} className="group inline-flex max-w-full items-center gap-1.5">
        <span className="font-mono text-[11px] font-bold tracking-tight text-coris-blue group-hover:text-coris-red">
          {row.original.numero}
        </span>
        <ArrowUpRight className="h-3.5 w-3.5 shrink-0 text-slate-300 transition-transform group-hover:-translate-y-0.5 group-hover:translate-x-0.5 group-hover:text-coris-red" />
      </Link>
    ),
  },
  {
    id: "dateDeclaration",
    accessorFn: (row) => row.dateDeclaration,
    header: "Période",
    meta: { className: "hidden w-[112px] sm:table-cell" } satisfies ColumnMeta,
    cell: ({ row }) => (
      <div className="space-y-0.5 text-xs leading-tight">
        <div className="font-semibold text-slate-700">{formatDate(row.original.dateDeclaration)}</div>
        <div className="text-slate-400">{row.original.dateFin ? `→ ${formatDate(row.original.dateFin)}` : "En cours"}</div>
      </div>
    ),
  },
  {
    id: "duree",
    header: "Durée",
    meta: { className: "hidden w-[110px] md:table-cell" } satisfies ColumnMeta,
    cell: ({ row }) => (
      <span className="inline-flex items-center gap-1 rounded-full bg-slate-100 px-2.5 py-1 text-xs font-semibold text-slate-600">
        <Clock3 className="h-3.5 w-3.5 text-coris-blue" />
        {formatDuration(row.original.dureeMinutes, row.original.dateDeclaration)}
      </span>
    ),
  },
  {
    id: "typeIncident.nom",
    accessorFn: (row) => row.typeIncident?.nom,
    header: "Type",
    meta: { className: "hidden w-[145px] xl:table-cell" } satisfies ColumnMeta,
    cell: ({ row }) => <span className="text-xs font-medium text-slate-600">{row.original.typeIncident?.nom || "—"}</span>,
  },
  {
    id: "application.nom",
    accessorFn: (row) => row.application?.nom,
    header: "Application",
    meta: { className: "hidden w-[150px] lg:table-cell" } satisfies ColumnMeta,
    cell: ({ row }) => <span className="text-xs font-semibold text-slate-700">{row.original.application?.nom || "—"}</span>,
  },
  {
    accessorKey: "intitule",
    header: "Intitulé de l'incident",
    meta: { className: "min-w-0" } satisfies ColumnMeta,
    cell: ({ row }) => {
      const incident = row.original;
      return (
        <div className="min-w-0 space-y-1">
          <Link
            href={`/incidents/${incident.id}`}
            title={incident.intitule}
            className="block max-w-full break-words text-sm font-semibold leading-5 text-slate-800 transition-colors hover:text-coris-blue line-clamp-2"
          >
            {incident.intitule}
          </Link>
          <div className="flex flex-wrap items-center gap-x-2 gap-y-1 text-[11px] text-slate-400 sm:hidden">
            <span>{incident.application?.nom || "Application non renseignée"}</span>
            <span aria-hidden="true">•</span>
            <span>{incident.typeIncident?.nom || "Type non renseigné"}</span>
          </div>
          <div className="hidden max-w-full truncate text-[11px] text-slate-400 xl:block">
            N1 : {incident.responsableN1?.nom || "Non affecté"}
            {incident.responsableN2?.nom ? ` · N2 : ${incident.responsableN2.nom}` : ""}
          </div>
        </div>
      );
    },
  },
  {
    id: "criticite.nom",
    accessorFn: (row) => row.criticite?.nom,
    header: "Criticité",
    meta: { className: "hidden w-[112px] md:table-cell" } satisfies ColumnMeta,
    cell: ({ row }) => <span className={criticiteClass(row.original.criticite?.nom || "")}>{row.original.criticite?.nom || "—"}</span>,
  },
  {
    id: "statut.nom",
    accessorFn: (row) => row.statut?.nom,
    header: "Statut",
    meta: { className: "hidden w-[112px] sm:table-cell" } satisfies ColumnMeta,
    cell: ({ row }) => <span className={statutClass(row.original.statut?.nom || "")}>{row.original.statut?.nom || "—"}</span>,
  },
  {
    id: "actions",
    header: "",
    meta: { className: "w-[78px] text-right" } satisfies ColumnMeta,
    enableSorting: false,
    cell: ({ row }) => (
      <Link
        href={`/incidents/${row.original.id}`}
        aria-label={`Ouvrir ${row.original.numero}`}
        className="ml-auto inline-flex h-9 w-9 items-center justify-center rounded-xl border border-slate-200 text-slate-400 transition-all hover:border-coris-blue hover:bg-coris-blue hover:text-white"
      >
        <ArrowUpRight className="h-4 w-4" />
      </Link>
    ),
  },
];
