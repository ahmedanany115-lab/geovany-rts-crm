"use client";

import { useState, useEffect } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { useAuthStore } from "@/stores/auth-store";
import { useErpDashboardKpis } from "@/features/erp/hooks";
import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api-client";
import { useT } from "@/hooks/useT";
import {
  Plus, TrendingUp, ShoppingCart, Users, FileCheck2,
  AlertTriangle, Clock, ChevronRight, ArrowRight,
  Package, ClipboardList,
} from "lucide-react";

/* ── helpers ── */
const fmtEGP = (v: number) =>
  v.toLocaleString("en-EG", { style: "currency", currency: "EGP", maximumFractionDigits: 0 });

function greet(firstName: string) {
  const h = new Date().getHours();
  if (h < 12) return `Good Morning, ${firstName} 👋`;
  if (h < 17) return `Good Afternoon, ${firstName} 👋`;
  return `Good Evening, ${firstName} 👋`;
}

function useClock() {
  const [time, setTime] = useState(new Date());
  useEffect(() => { const t = setInterval(() => setTime(new Date()), 1000); return () => clearInterval(t); }, []);
  return time;
}

/* ── component ── */
export default function ErpDashboardPage() {
  const { t, lang } = useT();
  const user   = useAuthStore(s => s.user);
  const router = useRouter();
  const now    = useClock();

  const { data: kpis, isLoading: kpiLoading } = useErpDashboardKpis();

  const { data: visits } = useQuery({
    queryKey: ["maintenance-visits-upcoming"],
    queryFn: () => apiFetch<any[]>(`/maintenance/visits?status=1&fromDate=${new Date().toISOString().split("T")[0]}`),
  });

  const { data: overdueInvoices } = useQuery({
    queryKey: ["overdue-invoices"],
    queryFn: () => apiFetch<any[]>("/customerinvoices?status=2"),
  });

  const { data: pendingSOs } = useQuery({
    queryKey: ["pending-sales-orders"],
    queryFn: () => apiFetch<any[]>("/salesorders?status=1"),
  });

  const { data: pendingPOs } = useQuery({
    queryKey: ["pending-purchase-orders"],
    queryFn: () => apiFetch<any[]>("/purchaseorders?status=1"),
  });

  const { data: notifications } = useQuery({
    queryKey: ["notifications"],
    queryFn: () => apiFetch<any[]>("/notifications"),
    refetchInterval: 30000,
  });

  const unreadNotifs = (notifications ?? []).filter((n: any) => !n.isRead).slice(0, 5);

  const dateStr = now.toLocaleDateString(lang === "ar" ? "ar-EG" : "en-GB", {
    weekday: "long", year: "numeric", month: "long", day: "numeric",
  });
  const timeStr = now.toLocaleTimeString(lang === "ar" ? "ar-EG" : "en-GB", {
    hour: "2-digit", minute: "2-digit", second: "2-digit",
  });

  /* ── Quick Actions ── */
  const quickActions = [
    { label: lang === "ar" ? "+ عميل جديد" : "+ New Customer",   href: "/crm/customers",  icon: Users,        color: "bg-blue-500" },
    { label: lang === "ar" ? "+ عميل محتمل" : "+ New Lead",       href: "/crm/leads",      icon: TrendingUp,   color: "bg-purple-500" },
    { label: lang === "ar" ? "+ عرض سعر"   : "+ New Quotation",  href: "/quotations",     icon: FileCheck2,   color: "bg-amber-500" },
    { label: lang === "ar" ? "+ أمر بيع"   : "+ New Sales Order",href: "/erp/sales-orders",icon: ShoppingCart,color: "bg-emerald-500" },
  ];

  /* ── Pipeline stages ── */
  const pipeline = [
    { label: "Leads",         value: "—",                                      icon: "🎯", href: "/crm/leads"              },
    { label: "Quotations",    value: String(pendingSOs?.length ?? "…"),         icon: "📋", href: "/quotations"             },
    { label: "Sales Orders",  value: String(kpis?.pendingSalesOrders ?? "…"),  icon: "📦", href: "/erp/sales-orders"       },
    { label: "Invoices",      value: String(overdueInvoices?.length ?? "…"),   icon: "🧾", href: "/erp/customer-invoices"  },
    { label: "Payments",      value: fmtEGP(kpis?.totalReceivables ?? 0),      icon: "💰", href: "/erp/payments"           },
  ];

  /* ── Needs attention ── */
  const attentionItems = [
    kpis && kpis.lowStockProducts > 0
      ? { label: `${kpis.lowStockProducts} products low on stock`, href: "/erp/products", type: "warning" }
      : null,
    overdueInvoices && overdueInvoices.length > 0
      ? { label: `${overdueInvoices.length} posted invoices awaiting payment`, href: "/erp/customer-invoices", type: "error" }
      : null,
    pendingSOs && pendingSOs.length > 0
      ? { label: `${pendingSOs.length} draft quotations awaiting confirmation`, href: "/erp/sales-orders", type: "info" }
      : null,
    pendingPOs && pendingPOs.length > 0
      ? { label: `${pendingPOs.length} purchase orders awaiting approval`, href: "/erp/purchase-orders", type: "info" }
      : null,
  ].filter(Boolean) as { label: string; href: string; type: string }[];

  /* ── Upcoming ── */
  const upcoming = (visits ?? []).slice(0, 5).map((v: any) => ({
    label: `Maintenance visit — ${v.customerName ?? ""}`,
    sub: v.scheduledDate,
    href: "/erp/maintenance/visits",
    icon: "🔧",
  }));

  return (
    <div className="p-6 space-y-6">

      {/* ── Header ── */}
      <div className="flex flex-col md:flex-row md:items-end md:justify-between gap-2">
        <div>
          <h1 className="text-2xl md:text-3xl font-bold">
            {user?.firstName ? greet(user.firstName) : "Welcome 👋"}
          </h1>
          <p className="text-muted-foreground text-sm mt-0.5">Here&apos;s what&apos;s happening today</p>
        </div>
        <div className="text-right">
          <p className="text-sm font-medium">{dateStr}</p>
          <p className="text-2xl font-mono font-semibold tabular-nums text-primary">{timeStr}</p>
        </div>
      </div>

      {/* ── Quick Actions ── */}
      <div>
        <h2 className="text-sm font-semibold text-muted-foreground uppercase tracking-wider mb-3">Quick Actions</h2>
        <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
          {quickActions.map(a => (
            <Link key={a.href} href={a.href}
              className="flex items-center gap-3 rounded-xl border p-4 hover:border-primary/40 hover:shadow-sm transition-all group bg-background">
              <div className={`h-9 w-9 rounded-lg ${a.color} flex items-center justify-center shrink-0 group-hover:scale-110 transition-transform`}>
                <a.icon className="h-4 w-4 text-white" />
              </div>
              <span className="text-sm font-semibold text-foreground">{a.label}</span>
            </Link>
          ))}
        </div>
      </div>

      {/* ── Sales Performance ── */}
      <div className="grid md:grid-cols-3 gap-4">
        {[
          { label: "Sales This Month", value: kpis ? fmtEGP(kpis.totalSalesThisMonth) : "…", icon: TrendingUp, color: "text-emerald-600", bg: "bg-emerald-50 dark:bg-emerald-950/20" },
          { label: "Sales This Year",  value: kpis ? fmtEGP(kpis.totalSalesThisYear)  : "…", icon: TrendingUp, color: "text-blue-600",    bg: "bg-blue-50 dark:bg-blue-950/20" },
          { label: "Pending Orders",   value: kpis ? String(kpis.pendingSalesOrders) + " orders" : "…", icon: ShoppingCart, color: "text-amber-600", bg: "bg-amber-50 dark:bg-amber-950/20" },
        ].map(k => (
          <div key={k.label} className={`rounded-xl border p-5 ${k.bg}`}>
            <div className="flex items-center gap-2 mb-2">
              <k.icon className={`h-5 w-5 ${k.color}`} />
              <p className="text-sm text-muted-foreground">{k.label}</p>
            </div>
            <p className={`text-2xl font-bold tabular-nums ${k.color}`}>
              {kpiLoading ? <span className="animate-pulse bg-muted rounded h-7 w-28 inline-block" /> : k.value}
            </p>
          </div>
        ))}
      </div>

      {/* ── Sales Pipeline ── */}
      <div className="card overflow-hidden">
        <div className="px-5 py-3 border-b flex items-center justify-between">
          <h2 className="font-semibold">Sales Pipeline</h2>
          <Link href="/erp/sales-orders" className="text-xs text-primary hover:underline flex items-center gap-1">
            View all <ChevronRight className="h-3 w-3" />
          </Link>
        </div>
        <div className="flex divide-x overflow-x-auto">
          {pipeline.map((stage, i) => (
            <Link key={stage.label} href={stage.href}
              className="flex-1 min-w-[120px] p-4 hover:bg-muted/30 transition-colors text-center group">
              <div className="flex items-center justify-center gap-1.5 mb-2">
                <span className="text-xl">{stage.icon}</span>
                {i < pipeline.length - 1 && (
                  <ArrowRight className="h-3 w-3 text-muted-foreground/40 absolute hidden" />
                )}
              </div>
              <p className="text-xs text-muted-foreground">{stage.label}</p>
              <p className="font-bold text-sm mt-0.5 group-hover:text-primary transition-colors">{stage.value}</p>
            </Link>
          ))}
        </div>
      </div>

      <div className="grid md:grid-cols-3 gap-5">

        {/* ── Needs Attention ── */}
        <div className="card overflow-hidden">
          <div className="px-5 py-3 border-b font-semibold flex items-center gap-2">
            <AlertTriangle className="h-4 w-4 text-amber-500" /> Needs Attention
          </div>
          <div className="divide-y">
            {attentionItems.length === 0 && (
              <div className="px-5 py-6 text-sm text-muted-foreground text-center">✅ Everything looks good!</div>
            )}
            {attentionItems.map((item, i) => (
              <Link key={i} href={item.href}
                className="flex items-center gap-3 px-4 py-3 hover:bg-muted/20 transition-colors">
                <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${
                  item.type === "error" ? "bg-red-100 text-red-700" :
                  item.type === "warning" ? "bg-amber-100 text-amber-700" : "bg-blue-100 text-blue-700"
                }`}>{item.type}</span>
                <p className="text-sm flex-1">{item.label}</p>
                <ChevronRight className="h-3.5 w-3.5 text-muted-foreground shrink-0" />
              </Link>
            ))}
          </div>
        </div>

        {/* ── Upcoming ── */}
        <div className="card overflow-hidden">
          <div className="px-5 py-3 border-b font-semibold flex items-center gap-2">
            <Clock className="h-4 w-4 text-blue-500" /> Upcoming
          </div>
          <div className="divide-y">
            {upcoming.length === 0 && (
              <div className="px-5 py-6 text-sm text-muted-foreground text-center">No upcoming items.</div>
            )}
            {upcoming.map((item, i) => (
              <Link key={i} href={item.href}
                className="flex items-center gap-3 px-4 py-3 hover:bg-muted/20 transition-colors">
                <span className="text-xl shrink-0">{item.icon}</span>
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium truncate">{item.label}</p>
                  <p className="text-xs text-muted-foreground">{item.sub}</p>
                </div>
              </Link>
            ))}
          </div>
        </div>

        {/* ── Notifications ── */}
        <div className="card overflow-hidden">
          <div className="px-5 py-3 border-b font-semibold flex items-center justify-between">
            <span>Notifications</span>
            {unreadNotifs.length > 0 && (
              <span className="text-xs bg-red-500 text-white px-1.5 py-0.5 rounded-full">{unreadNotifs.length}</span>
            )}
          </div>
          <div className="divide-y">
            {unreadNotifs.length === 0 && (
              <div className="px-5 py-6 text-sm text-muted-foreground text-center">All caught up! 🎉</div>
            )}
            {unreadNotifs.map((n: any) => (
              <Link key={n.id} href={n.relatedRoute ?? "#"}
                className="flex items-start gap-3 px-4 py-3 hover:bg-muted/20 transition-colors">
                <div className={`h-2 w-2 rounded-full mt-1.5 shrink-0 ${
                  n.type === "error" ? "bg-red-500" : n.type === "warning" ? "bg-amber-500" :
                  n.type === "success" ? "bg-emerald-500" : "bg-blue-500"
                }`} />
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium leading-tight truncate">{n.title}</p>
                  <p className="text-xs text-muted-foreground mt-0.5 line-clamp-2">{n.body}</p>
                </div>
              </Link>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
