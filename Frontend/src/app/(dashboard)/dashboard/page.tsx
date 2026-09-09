"use client";
import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api-client";
import { useErpDashboardKpis } from "@/features/erp/hooks";
import { useT } from "@/hooks/useT";
import { DollarSign, TrendingUp, AlertTriangle, CreditCard, Package, BarChart3, Users, CheckSquare } from "lucide-react";

export default function DashboardPage() {
  const { t } = useT();
  const { data: kpis, isLoading } = useErpDashboardKpis();
  const { data: systemUsers } = useQuery({ queryKey: ["system-users"], queryFn: () => apiFetch<{ id: string; fullName: string; roles: string[]; isActive: boolean }[]>("/users") });

  const fmt = (v: number) => v.toLocaleString("en-EG", { style: "currency", currency: "EGP", maximumFractionDigits: 0 });

  const cards = kpis ? [
    { label: t("total_revenue"),    value: fmt(kpis.totalSalesThisYear),    icon: TrendingUp,    color: "text-emerald-600", bg: "bg-emerald-50 dark:bg-emerald-950/30" },
    { label: t("outstanding_ar"),   value: fmt(kpis.totalReceivables),      icon: DollarSign,    color: "text-blue-600",    bg: "bg-blue-50 dark:bg-blue-950/30" },
    { label: t("outstanding_ap"),   value: fmt(kpis.totalPayables),         icon: CreditCard,    color: "text-red-600",     bg: "bg-red-50 dark:bg-red-950/30" },
    { label: t("inventory_value"),  value: fmt(kpis.inventoryValue),        icon: Package,       color: "text-purple-600",  bg: "bg-purple-50 dark:bg-purple-950/30" },
    { label: t("low_stock"),        value: String(kpis.lowStockProducts),   icon: AlertTriangle, color: "text-amber-600",   bg: "bg-amber-50 dark:bg-amber-950/30" },
    { label: t("open_cheques"),     value: fmt(kpis.outstandingCheques),    icon: CheckSquare,   color: "text-cyan-600",    bg: "bg-cyan-50 dark:bg-cyan-950/30" },
    { label: t("commissions"),      value: fmt(kpis.pendingCommission),     icon: BarChart3,     color: "text-indigo-600",  bg: "bg-indigo-50 dark:bg-indigo-950/30" },
    { label: t("system_users"),     value: String(systemUsers?.filter(u => u.isActive).length ?? "—"), icon: Users, color: "text-gray-600", bg: "bg-gray-50 dark:bg-gray-950/30" },
  ] : [];

  return (
    <div className="p-6 space-y-6">
      <h1 className="text-2xl font-semibold">{t("dashboard")}</h1>
      {isLoading ? (
        <div className="text-muted-foreground">{t("loading")}</div>
      ) : (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          {cards.map(c => (
            <div key={c.label} className={`card p-4 rounded-xl ${c.bg}`}>
              <div className="flex items-center gap-2 mb-2">
                <c.icon className={`h-5 w-5 ${c.color}`} />
                <p className={`text-sm font-medium ${c.color}`}>{c.label}</p>
              </div>
              <p className={`text-2xl font-bold tabular-nums ${c.color}`}>{c.value}</p>
            </div>
          ))}
        </div>
      )}
      <div className="grid md:grid-cols-2 gap-6">
        <div className="card p-4">
          <h2 className="font-semibold mb-3">{t("sales")} – {t("date")}</h2>
          <p className="text-3xl font-bold tabular-nums text-emerald-600">{kpis ? fmt(kpis.totalSalesThisMonth) : "—"}</p>
          <p className="text-sm text-muted-foreground mt-1">{t("total_revenue")} {t("period")}</p>
        </div>
        <div className="card p-4">
          <h2 className="font-semibold mb-3">{t("purchasing")} – {t("date")}</h2>
          <p className="text-3xl font-bold tabular-nums text-red-600">{kpis ? fmt(kpis.totalPurchasesThisMonth) : "—"}</p>
          <p className="text-sm text-muted-foreground mt-1">{t("total_expenses")} {t("period")}</p>
        </div>
      </div>
    </div>
  );
}
