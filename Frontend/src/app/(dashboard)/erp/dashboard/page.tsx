"use client";

import { TrendingUp, TrendingDown, DollarSign, ShoppingCart, ShoppingBag, Package, CreditCard, Building2, RefreshCw, AlertTriangle, CheckCircle } from "lucide-react";
import { useErpDashboardKpis } from "@/features/erp/hooks";

function KpiCard({ title, value, subtitle, icon: Icon, color = "text-foreground", trend }: {
  title: string; value: string; subtitle?: string;
  icon: React.ElementType; color?: string; trend?: "up" | "down" | "neutral";
}) {
  return (
    <div className="card p-4 space-y-2">
      <div className="flex items-center justify-between">
        <span className="text-sm text-muted-foreground">{title}</span>
        <Icon className={`h-5 w-5 ${color} opacity-70`} />
      </div>
      <div className={`text-2xl font-bold tabular-nums ${color}`}>{value}</div>
      {subtitle && <div className="text-xs text-muted-foreground">{subtitle}</div>}
      {trend && (
        <div className={`flex items-center gap-1 text-xs ${trend === "up" ? "text-emerald-600" : trend === "down" ? "text-red-600" : "text-muted-foreground"}`}>
          {trend === "up" ? <TrendingUp className="h-3 w-3" /> : trend === "down" ? <TrendingDown className="h-3 w-3" /> : null}
        </div>
      )}
    </div>
  );
}

const fmt = (v: number, currency = "EGP") =>
  v.toLocaleString("en-EG", { style: "currency", currency, maximumFractionDigits: 0 });

export default function ErpDashboardPage() {
  const { data: kpis, isLoading, isError, refetch } = useErpDashboardKpis();

  return (
    <div className="p-6 space-y-8">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold">Finance & ERP Dashboard</h1>
          <p className="text-sm text-muted-foreground mt-0.5">Live financial and operational overview</p>
        </div>
        <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg" title="Refresh">
          <RefreshCw className="h-4 w-4" />
        </button>
      </div>

      {isLoading && (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          {Array.from({ length: 8 }).map((_, i) => (
            <div key={i} className="h-28 rounded-lg border bg-accent/30 animate-pulse" />
          ))}
        </div>
      )}

      {isError && (
        <div className="flex items-center gap-3 rounded-lg border border-amber-200 bg-amber-50 dark:bg-amber-950/20 p-4 text-sm">
          <AlertTriangle className="h-5 w-5 text-amber-600 shrink-0" />
          <div>
            <p className="font-medium text-amber-800 dark:text-amber-200">Could not load KPIs</p>
            <p className="text-amber-600 dark:text-amber-300 text-xs mt-0.5">Ensure the backend API is running and the database is migrated.</p>
          </div>
          <button onClick={() => refetch()} className="ml-auto text-xs underline text-amber-700">Retry</button>
        </div>
      )}

      {kpis && (
        <>
          {/* Sales KPIs */}
          <section className="space-y-3">
            <h2 className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">Sales</h2>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
              <KpiCard
                title="Sales This Month" value={fmt(kpis.totalSalesThisMonth)}
                icon={TrendingUp} color="text-emerald-600" trend="up"
              />
              <KpiCard
                title="Sales This Year" value={fmt(kpis.totalSalesThisYear)}
                icon={DollarSign} color="text-emerald-700"
              />
              <KpiCard
                title="Accounts Receivable" value={fmt(kpis.totalReceivables)}
                subtitle="Outstanding customer balances"
                icon={ShoppingCart} color={kpis.totalReceivables > 0 ? "text-amber-600" : "text-emerald-600"}
              />
              <KpiCard
                title="Pending Sales Orders" value={kpis.pendingSalesOrders.toString()}
                subtitle="Awaiting approval or delivery"
                icon={CheckCircle} color="text-blue-600"
              />
            </div>
          </section>

          {/* Purchasing KPIs */}
          <section className="space-y-3">
            <h2 className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">Purchasing</h2>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
              <KpiCard
                title="Purchases This Month" value={fmt(kpis.totalPurchasesThisMonth)}
                icon={TrendingDown} color="text-red-600"
              />
              <KpiCard
                title="Accounts Payable" value={fmt(kpis.totalPayables)}
                subtitle="Outstanding supplier balances"
                icon={ShoppingBag} color={kpis.totalPayables > 0 ? "text-red-600" : "text-emerald-600"}
              />
              <KpiCard
                title="Pending POs" value={kpis.pendingPurchaseOrders.toString()}
                subtitle="Awaiting approval or receipt"
                icon={ShoppingBag} color="text-blue-600"
              />
              <KpiCard
                title="Inventory Value" value={fmt(kpis.inventoryValue)}
                subtitle="Based on average cost"
                icon={Package} color="text-indigo-600"
              />
            </div>
          </section>

          {/* Banking & Cheques */}
          <section className="space-y-3">
            <h2 className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">Banking & Cheques</h2>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
              {kpis.bankBalances?.map(b => (
                <KpiCard key={`${b.bankName}-${b.currency}`}
                  title={`${b.bankName} (${b.currency})`}
                  value={b.balance.toLocaleString("en-EG", { style: "currency", currency: b.currency, maximumFractionDigits: 0 })}
                  icon={Building2} color={b.balance >= 0 ? "text-emerald-600" : "text-red-600"}
                />
              ))}
              <KpiCard
                title="Outstanding Cheques" value={fmt(kpis.outstandingCheques)}
                subtitle={`${kpis.outstandingChequeCount} cheques received`}
                icon={CreditCard} color="text-amber-600"
              />
              <KpiCard
                title="Low Stock Products" value={kpis.lowStockProducts.toString()}
                subtitle="At or below minimum stock"
                icon={AlertTriangle} color={kpis.lowStockProducts > 0 ? "text-orange-600" : "text-emerald-600"}
              />
            </div>
          </section>

          {/* Commission */}
          <section className="space-y-3">
            <h2 className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">Commission</h2>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
              <KpiCard
                title="Pending Commission" value={fmt(kpis.pendingCommission)}
                subtitle="1.5% of sales — awaiting approval"
                icon={DollarSign} color="text-purple-600"
              />
            </div>
          </section>

          {/* Quick workflow guide */}
          <section className="space-y-3">
            <h2 className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">Demo Workflow</h2>
            <div className="grid grid-cols-2 gap-4">
              <div className="card p-4 space-y-2">
                <h3 className="font-medium text-sm flex items-center gap-2">
                  <TrendingUp className="h-4 w-4 text-emerald-600" /> Sales Cycle
                </h3>
                <ol className="text-xs text-muted-foreground space-y-1 list-decimal list-inside">
                  <li>Create Customer</li>
                  <li>Create Sales Order (VAT 14% auto-calculated)</li>
                  <li>Approve Sales Order</li>
                  <li>Create Delivery → decrements inventory, posts COGS journal</li>
                  <li>Create Customer Invoice → posts Dr AR / Cr Revenue / Cr VAT</li>
                  <li>Sales Commission 1.5% auto-created on posting</li>
                  <li>Record Payment (Bank/Cheque) → posts Dr Bank / Cr AR</li>
                  <li>View Journal Entries → balanced debit = credit</li>
                </ol>
              </div>
              <div className="card p-4 space-y-2">
                <h3 className="font-medium text-sm flex items-center gap-2">
                  <TrendingDown className="h-4 w-4 text-red-600" /> Purchase Cycle
                </h3>
                <ol className="text-xs text-muted-foreground space-y-1 list-decimal list-inside">
                  <li>Create Supplier</li>
                  <li>Create Product + Warehouse</li>
                  <li>Create Purchase Order (VAT 14% on gross)</li>
                  <li>Approve Purchase Order</li>
                  <li>Create Purchase Receipt → increases inventory balance</li>
                  <li>Create Supplier Invoice → posts Dr Purchase / Dr VAT / Cr AP</li>
                  <li>Record Supplier Payment → posts Dr AP / Cr Bank</li>
                  <li>View Trial Balance → confirms balanced books</li>
                </ol>
              </div>
            </div>
          </section>
        </>
      )}
    </div>
  );
}
