"use client";

import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api-client";
import { useErpDashboardKpis } from "@/features/erp/hooks";
import { useT } from "@/hooks/useT";
import { useRoles } from "@/hooks/useRoles";
import {
  TrendingUp, DollarSign, CreditCard, Package,
  AlertTriangle, CheckSquare, BarChart3, Users, RefreshCw,
} from "lucide-react";
import Link from "next/link";

const fmt = (v: number) => v.toLocaleString("en-EG", { style: "currency", currency: "EGP", maximumFractionDigits: 0 });

function KpiCard({ title, value, subtitle, icon: Icon, color, bg, href }: {
  title: string; value: string; subtitle?: string;
  icon: React.ElementType; color: string; bg: string; href?: string;
}) {
  const inner = (
    <div className={`card p-5 rounded-xl border ${bg} hover:shadow-md transition-shadow`}>
      <div className="flex items-center justify-between mb-3">
        <p className="text-sm font-medium text-muted-foreground">{title}</p>
        <div className={`h-9 w-9 rounded-lg flex items-center justify-center ${bg.replace("bg-", "bg-").replace("/20", "/40")}`}>
          <Icon className={`h-5 w-5 ${color}`} />
        </div>
      </div>
      <p className={`text-2xl font-bold tabular-nums ${color}`}>{value}</p>
      {subtitle && <p className="text-xs text-muted-foreground mt-1">{subtitle}</p>}
    </div>
  );
  return href ? <Link href={href}>{inner}</Link> : inner;
}

export default function FinanceDashboardPage() {
  const { t } = useT();
  const { isAdmin, hasRole } = useRoles();
  const { data: kpis, isLoading, refetch } = useErpDashboardKpis();

  const { data: systemUsers } = useQuery({
    queryKey: ["system-users-count"],
    queryFn: () => apiFetch<{ id: string; isActive: boolean }[]>("/users"),
    enabled: isAdmin || hasRole("Manager"),
  });

  const activeUserCount = systemUsers?.filter(u => u.isActive).length ?? 0;

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Finance Dashboard</h1>
          <p className="text-sm text-muted-foreground mt-0.5">Live financial overview from accounting data</p>
        </div>
        <button onClick={() => refetch()} disabled={isLoading}
          className="btn-ghost p-2 rounded-lg disabled:opacity-50">
          <RefreshCw className={`h-4 w-4 ${isLoading ? "animate-spin" : ""}`} />
        </button>
      </div>

      {isLoading ? (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          {Array.from({ length: 8 }).map((_, i) => (
            <div key={i} className="card p-5 animate-pulse">
              <div className="h-4 w-24 bg-muted rounded mb-3" />
              <div className="h-7 w-32 bg-muted rounded" />
            </div>
          ))}
        </div>
      ) : (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <KpiCard
            title={t("total_revenue")}
            value={fmt(kpis?.totalSalesThisYear ?? 0)}
            subtitle="This year"
            icon={TrendingUp}
            color="text-emerald-600"
            bg="bg-emerald-50 dark:bg-emerald-950/20"
            href="/finance/income-statement"
          />
          <KpiCard
            title={t("outstanding_ar")}
            value={fmt(kpis?.totalReceivables ?? 0)}
            subtitle="Unpaid receivables"
            icon={DollarSign}
            color="text-blue-600"
            bg="bg-blue-50 dark:bg-blue-950/20"
            href="/erp/customer-invoices"
          />
          <KpiCard
            title={t("outstanding_ap")}
            value={fmt(kpis?.totalPayables ?? 0)}
            subtitle="Outstanding payables"
            icon={CreditCard}
            color="text-red-600"
            bg="bg-red-50 dark:bg-red-950/20"
            href="/erp/supplier-invoices"
          />
          <KpiCard
            title={t("inventory_value")}
            value={fmt(kpis?.inventoryValue ?? 0)}
            subtitle="Current stock value"
            icon={Package}
            color="text-purple-600"
            bg="bg-purple-50 dark:bg-purple-950/20"
            href="/erp/inventory"
          />
          <KpiCard
            title={t("low_stock")}
            value={String(kpis?.lowStockProducts ?? 0)}
            subtitle="Products below min stock"
            icon={AlertTriangle}
            color="text-amber-600"
            bg="bg-amber-50 dark:bg-amber-950/20"
            href="/erp/products"
          />
          <KpiCard
            title={t("open_cheques")}
            value={fmt(kpis?.outstandingCheques ?? 0)}
            subtitle={`${kpis?.outstandingChequeCount ?? 0} cheques`}
            icon={CheckSquare}
            color="text-cyan-600"
            bg="bg-cyan-50 dark:bg-cyan-950/20"
            href="/erp/cheques"
          />
          <KpiCard
            title={t("commissions")}
            value={fmt(kpis?.pendingCommission ?? 0)}
            subtitle="Pending commissions"
            icon={BarChart3}
            color="text-indigo-600"
            bg="bg-indigo-50 dark:bg-indigo-950/20"
          />
          {(isAdmin || hasRole("Manager")) && (
            <KpiCard
              title={t("system_users")}
              value={String(activeUserCount)}
              subtitle="Active users"
              icon={Users}
              color="text-gray-600"
              bg="bg-gray-50 dark:bg-gray-950/20"
              href="/users/employees"
            />
          )}
        </div>
      )}

      {/* Quick finance links */}
      <div>
        <h2 className="text-sm font-semibold text-muted-foreground uppercase tracking-wider mb-3">Finance Modules</h2>
        <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
          {[
            { label: "Journal Entries",   href: "/finance/journal-entries", icon: "📒" },
            { label: "Ledger",            href: "/finance/ledger",          icon: "📊" },
            { label: "Trial Balance",     href: "/finance/trial-balance",   icon: "⚖️" },
            { label: "Income Statement",  href: "/finance/income-statement",icon: "📈" },
            { label: "Profit & Loss",     href: "/finance/profit-loss",     icon: "💹" },
            { label: "Chart of Accounts", href: "/finance/accounts",        icon: "🏦" },
            { label: "Fiscal Periods",    href: "/finance/fiscal-periods",  icon: "📅" },
            { label: "Bank Accounts",     href: "/erp/bank-accounts",       icon: "🏛️" },
          ].map(l => (
            <Link key={l.href} href={l.href}
              className="flex items-center gap-3 rounded-xl border p-3 hover:border-primary/40 hover:bg-muted/30 transition-all text-sm">
              <span className="text-xl">{l.icon}</span>
              <span className="font-medium text-foreground">{l.label}</span>
            </Link>
          ))}
        </div>
      </div>

      {/* Bank balances */}
      {kpis?.bankBalances && kpis.bankBalances.length > 0 && (
        <div>
          <h2 className="text-sm font-semibold text-muted-foreground uppercase tracking-wider mb-3">Bank Balances</h2>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
            {kpis.bankBalances.map((b, i) => (
              <div key={i} className="card p-4 rounded-xl">
                <p className="text-xs text-muted-foreground">{b.bankName}</p>
                <p className="text-lg font-bold tabular-nums">{fmt(b.balance)}</p>
                <p className="text-xs text-muted-foreground mt-0.5">{b.currency}</p>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
