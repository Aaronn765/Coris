"use client";

import Image from "next/image";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { AlertCircle, FolderKanban, LayoutDashboard, Settings, type LucideIcon } from "lucide-react";
import { FC, ReactNode } from "react";

const navigation: { href: string; label: string; shortLabel: string; icon: LucideIcon }[] = [
  { href: "/", label: "Tableau de bord", shortLabel: "Accueil", icon: LayoutDashboard },
  { href: "/incidents", label: "Incidents", shortLabel: "Incidents", icon: AlertCircle },
  { href: "/projets", label: "Projets", shortLabel: "Projets", icon: FolderKanban },
  { href: "/administration", label: "Paramètres", shortLabel: "Réglages", icon: Settings },
];

export const AppLayout: FC<{ children: ReactNode }> = ({ children }) => {
  const pathname = usePathname();
  const isActive = (href: string) => (href === "/" ? pathname === href : pathname.startsWith(href));

  return (
    <div className="min-h-screen bg-transparent md:flex">
      <aside className="bg-coris-sidebar relative z-20 hidden w-64 shrink-0 flex-col text-white md:flex">
        <div className="relative border-b border-white/10 bg-white px-4 py-5">
          <div className="absolute inset-x-0 bottom-0 h-1 bg-coris-red" />
          <Image src="/logo.png" alt="Logo de la banque" width={176} height={74} className="h-auto w-full object-contain" priority />
        </div>
        <nav aria-label="Navigation principale" className="flex-1 px-5 py-6">
          <div className="relative space-y-2 border-l border-white/15 pl-3">
          {navigation.map(({ href, label, icon: Icon }) => {
            const active = isActive(href);
            return (
              <Link key={href} href={href} aria-current={active ? "page" : undefined} className={`group relative flex items-center gap-3 rounded-r-2xl px-3 py-3.5 text-sm font-semibold transition-all duration-200 ${active ? "bg-white/[0.13] text-white shadow-lg shadow-blue-950/10" : "text-blue-100/75 hover:bg-white/[0.08] hover:text-white"}`}>
                {active && <span className="absolute -left-[13px] inset-y-1 w-1 rounded-full bg-coris-red shadow-[0_0_14px_rgba(227,27,45,0.8)]" />}
                <span className={`flex h-9 w-9 items-center justify-center rounded-xl transition-colors ${active ? "bg-white text-coris-blue" : "bg-white/10 text-blue-100 group-hover:bg-white/15"}`}><Icon className="h-[18px] w-[18px]" /></span>
                {label}
              </Link>
            );
          })}
          </div>
        </nav>
      </aside>

      <main className="flex min-h-screen min-w-0 flex-1 flex-col">
        <div className="min-w-0 flex-1 overflow-auto px-4 pb-24 pt-5 sm:px-6 lg:px-8 lg:pt-7 md:pb-7">{children}</div>
      </main>

      <nav aria-label="Navigation mobile" className="fixed inset-x-0 bottom-0 z-50 flex h-[4.5rem] border-t border-slate-200 bg-white/95 px-2 shadow-[0_-8px_28px_rgba(8,45,111,0.1)] backdrop-blur-xl md:hidden">
        {navigation.map(({ href, shortLabel, icon: Icon }) => {
          const active = isActive(href);
          return <Link key={href} href={href} aria-current={active ? "page" : undefined} className={`relative flex min-w-0 flex-1 flex-col items-center justify-center gap-1 text-[11px] font-semibold transition-colors ${active ? "text-coris-blue" : "text-slate-400"}`}>{active && <span className="absolute top-0 h-0.5 w-10 rounded-full bg-coris-red" />}<Icon className="h-5 w-5" strokeWidth={active ? 2.5 : 2} /><span className="truncate">{shortLabel}</span></Link>;
        })}
      </nav>
    </div>
  );
};
