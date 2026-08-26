"use client";

import * as React from "react";
import {
  ColumnDef,
  flexRender,
  getCoreRowModel,
  getFilteredRowModel,
  getPaginationRowModel,
  getSortedRowModel,
  OnChangeFn,
  PaginationState,
  SortingState,
  useReactTable,
} from "@tanstack/react-table";
import { ChevronLeft, ChevronRight, Loader2 } from "lucide-react";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Button } from "@/components/ui/button";

interface DataTableProps<TData, TValue> {
  columns: ColumnDef<TData, TValue>[];
  data: TData[];
  loading?: boolean;
  serverState?: {
    pageIndex: number;
    pageSize: number;
    pageCount: number;
    onPageChange: (pageIndex: number) => void;
    sorting: SortingState;
    onSortingChange: OnChangeFn<SortingState>;
    globalFilter: string;
    onGlobalFilterChange: OnChangeFn<string>;
  };
}

type ColumnMeta = { className?: string };

export function DataTable<TData, TValue>({ columns, data, loading = false, serverState }: DataTableProps<TData, TValue>) {
  const [sorting, setSorting] = React.useState<SortingState>([]);
  const [globalFilter, setGlobalFilter] = React.useState("");
  const pagination: PaginationState = serverState
    ? { pageIndex: serverState.pageIndex, pageSize: serverState.pageSize }
    : { pageIndex: 0, pageSize: 10 };

  const table = useReactTable({
    data,
    columns,
    getCoreRowModel: getCoreRowModel(),
    getPaginationRowModel: serverState ? undefined : getPaginationRowModel(),
    onSortingChange: serverState ? serverState.onSortingChange : setSorting,
    getSortedRowModel: serverState ? undefined : getSortedRowModel(),
    getFilteredRowModel: serverState ? undefined : getFilteredRowModel(),
    onGlobalFilterChange: serverState ? serverState.onGlobalFilterChange : setGlobalFilter,
    manualPagination: Boolean(serverState),
    manualSorting: Boolean(serverState),
    manualFiltering: Boolean(serverState),
    pageCount: serverState?.pageCount,
    state: {
      sorting: serverState?.sorting ?? sorting,
      globalFilter: serverState?.globalFilter ?? globalFilter,
      pagination,
    },
    onPaginationChange: serverState
      ? (updater) => {
          const next = typeof updater === "function" ? updater(pagination) : updater;
          serverState.onPageChange(next.pageIndex);
        }
      : undefined,
  });

  return (
    <div className="relative">
      {loading && (
        <div className="absolute inset-x-0 top-0 z-10 h-0.5 overflow-hidden rounded-full bg-coris-blue/10">
          <div className="h-full w-1/3 animate-[coris-loading_1.2s_ease-in-out_infinite] rounded-full bg-coris-red" />
        </div>
      )}
      <div className="overflow-hidden rounded-xl border border-slate-200/80 bg-white shadow-[0_18px_50px_rgba(13,55,122,0.07)] sm:rounded-2xl">
        <Table className="table-fixed">
          <TableHeader className="bg-slate-50/90">
            {table.getHeaderGroups().map((headerGroup) => (
              <TableRow key={headerGroup.id} className="border-slate-200 hover:bg-transparent">
                {headerGroup.headers.map((header) => {
                  const meta = header.column.columnDef.meta as ColumnMeta | undefined;
                  return (
                    <TableHead key={header.id} className={`h-12 px-3 text-[10px] font-bold uppercase tracking-[0.12em] text-slate-400 ${meta?.className || ""}`}>
                      {header.isPlaceholder ? null : (
                        <button
                          type="button"
                          className={`inline-flex items-center gap-1 text-left ${header.column.getCanSort() ? "cursor-pointer select-none hover:text-coris-blue" : ""}`}
                          onClick={header.column.getToggleSortingHandler()}
                        >
                          {flexRender(header.column.columnDef.header, header.getContext())}
                          {header.column.getIsSorted() === "asc" ? " ↑" : header.column.getIsSorted() === "desc" ? " ↓" : ""}
                        </button>
                      )}
                    </TableHead>
                  );
                })}
              </TableRow>
            ))}
          </TableHeader>
          <TableBody>
            {table.getRowModel().rows.length ? (
              table.getRowModel().rows.map((row) => (
                <TableRow key={row.id} className="border-slate-100 bg-white hover:bg-coris-blue/[0.025]">
                  {row.getVisibleCells().map((cell) => {
                    const meta = cell.column.columnDef.meta as ColumnMeta | undefined;
                    return (
                      <TableCell key={cell.id} className={`min-w-0 whitespace-normal px-3 py-3.5 align-top ${meta?.className || ""}`}>
                        {flexRender(cell.column.columnDef.cell, cell.getContext())}
                      </TableCell>
                    );
                  })}
                </TableRow>
              ))
            ) : (
              <TableRow className="hover:bg-white">
                <TableCell colSpan={columns.length} className="h-48 whitespace-normal text-center">
                  <div className="mx-auto flex max-w-xs flex-col items-center gap-2 text-slate-400">
                    <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-coris-blue-soft text-coris-blue">—</div>
                    <p className="font-semibold text-slate-600">Aucun incident trouvé</p>
                    <p className="text-xs">Modifiez les filtres ou la recherche pour élargir les résultats.</p>
                  </div>
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>
      <div className="grid gap-3 px-1 py-4 text-xs text-slate-500 sm:flex sm:flex-wrap sm:items-center sm:justify-between">
        <span className="text-center sm:text-left">Page <strong className="text-slate-700">{(serverState?.pageIndex ?? table.getState().pagination.pageIndex) + 1}</strong> sur <strong className="text-slate-700">{Math.max(serverState?.pageCount ?? table.getPageCount(), 1)}</strong></span>
        <div className="grid grid-cols-2 gap-2 sm:flex sm:items-center">
          <Button variant="outline" size="sm" className="h-9 rounded-xl border-slate-200" onClick={() => table.previousPage()} disabled={!table.getCanPreviousPage()}>
            <ChevronLeft className="mr-1 h-4 w-4" /> Précédent
          </Button>
          <Button variant="outline" size="sm" className="h-9 rounded-xl border-slate-200" onClick={() => table.nextPage()} disabled={!table.getCanNextPage()}>
            Suivant <ChevronRight className="ml-1 h-4 w-4" />
          </Button>
        </div>
      </div>
      {loading && data.length > 0 && <Loader2 className="absolute bottom-5 left-1/2 h-4 w-4 animate-spin text-coris-red" />}
    </div>
  );
}
