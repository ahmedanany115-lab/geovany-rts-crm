"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  LayoutDashboard, Users, FileText, Briefcase, KanbanSquare, LifeBuoy,
  Package, Receipt, BarChart3, UserCog, Settings, ChevronsLeft, ChevronsRight,
  Landmark, ShoppingCart, ShoppingBag, Truck, CreditCard, DollarSign,
  Warehouse, TrendingUp, BookOpen, ArrowLeftRight, CalendarDays, Coins,
  HeartHandshake,
} from "lucide-react";
import { cn } from "@/lib/utils";
import { useUiStore } from "@/stores/ui-state";
import { useT } from "@/hooks/useT";
import { useRoles } from "@/hooks/useRoles";
import { useState } from "react";

export function Sidebar() {
  const pathname = usePathname();
  const { sidebarCollapsed, toggleSidebar } = useUiStore();
  const { t, lang } = useT();
  const { isFinance, isAdmin, hasRole } = useRoles();
  const [erpExpanded, setErpExpanded] = useState(
    pathname?.startsWith("/erp") || pathname?.startsWith("/finance")
  );

  const isActive = (href: string) =>
    pathname === href || pathname?.startsWith(href + "/");

  const NavLink = ({
    href, label, icon: Icon,
  }: { href: string; label: string; icon: React.ComponentType<{ className?: string }> }) => {
    const active = isActive(href);
    return (
      <Link href={href}
        className={cn(
          "flex items-center gap-3 rounded-md px-3 py-2 text-sm transition-colors",
          active
            ? "bg-accent font-medium text-accent-foreground"
            : "text-muted-foreground hover:bg-accent/50"
        )}>
        <Icon className="h-4 w-4 shrink-0" />
        {!sidebarCollapsed && <span>{label}</span>}
      </Link>
    );
  };

  const SectionLabel = ({ text }: { text: string }) =>
    !sidebarCollapsed ? (
      <div className="px-3 py-1 text-xs font-semibold uppercase tracking-wider text-muted-foreground/60">
        {text}
      </div>
    ) : null;

  // Roles that can see Finance & ERP section
  const showErp = isFinance ||
    hasRole("Manager", "Sales", "Purchasing", "Delivery", "SupportAgent", "Marketing", "ReadOnly");

  // Which sub-sections each role can see inside ERP
  const showFinanceGroup = isFinance;   // Admin, Accountant, SalesManager only
  const showSalesGroup   = hasRole("Admin","Manager","SalesManager","Sales","Accountant","Delivery");
  const showPurchasingGroup = hasRole("Admin","Manager","Accountant","Purchasing","SalesManager");
  const showInventoryGroup  = hasRole("Admin","Manager","SalesManager","Sales","Accountant","Purchasing","Delivery");

  return (
    <aside className={cn(
      "flex h-screen flex-col border-r bg-background transition-all duration-200",
      sidebarCollapsed ? "w-16" : "w-60",
      lang === "ar" ? "border-l border-r-0" : ""
    )}>
      {/* Logo */}
      <div className="flex h-14 items-center gap-2 px-4 font-semibold text-primary shrink-0">
        <div className="flex h-7 w-7 items-center justify-center rounded-md bg-primary text-sm font-bold text-primary-foreground shrink-0">
          R
        </div>
        {!sidebarCollapsed && <span>Royal ERP</span>}
      </div>

      <nav className="flex-1 overflow-y-auto px-2 py-1 space-y-0.5">

        {/* ── Top level ── */}
        <NavLink href="/dashboard"      label={t("dashboard")} icon={LayoutDashboard} />
        <NavLink href="/crm/customers"  label={t("crm")}       icon={Users} />
        <NavLink href="/projects"       label={t("projects")}  icon={Briefcase} />
        <NavLink href="/tasks/board"    label={t("tasks")}     icon={KanbanSquare} />
        <NavLink href="/helpdesk"       label={t("helpdesk")}  icon={LifeBuoy} />

        {/* ── Self Service (everyone) ── */}
        <div className="mt-3 pt-2 border-t border-border/50">
          <SectionLabel text={t("self_service")} />
          <NavLink href="/hr/leaves"   label={t("leaves")}   icon={HeartHandshake} />
          <NavLink href="/hr/meetings" label={t("meetings")} icon={CalendarDays} />
        </div>

        {/* ── Finance & ERP toggle ── */}
        {showErp && (
          <>
            <button
              onClick={() => setErpExpanded(v => !v)}
              className={cn(
                "w-full flex items-center gap-3 rounded-md px-3 py-2 text-sm transition-colors mt-2",
                erpExpanded ? "text-primary font-medium" : "text-muted-foreground hover:bg-accent/50"
              )}>
              <TrendingUp className="h-4 w-4 shrink-0" />
              {!sidebarCollapsed && (
                <>
                  <span className="flex-1 text-left">
                    {lang === "ar" ? "المالية والـ ERP" : "Finance & ERP"}
                  </span>
                  <span className="text-xs opacity-60">{erpExpanded ? "▲" : "▼"}</span>
                </>
              )}
            </button>

            {erpExpanded && (
              <>
                {/* ERP Overview */}
                <div className="mt-1">
                  <div className={!sidebarCollapsed ? "ml-2" : ""}>
                    <NavLink href="/erp/dashboard" label={t("erp_dashboard")} icon={TrendingUp} />
                  </div>
                </div>

                {/* Finance — Admin, Accountant, SalesManager ONLY */}
                {showFinanceGroup && (
                  <div className="mt-1">
                    <SectionLabel text={t("finance")} />
                    {[
                      { href: "/finance/accounts",       label: t("chart_of_accounts"), icon: BookOpen },
                      { href: "/finance/journal-entries", label: t("journal_entries"),  icon: FileText },
                      { href: "/finance/ledger",          label: t("ledger"),            icon: Receipt },
                      { href: "/finance/trial-balance",   label: t("trial_balance"),     icon: BarChart3 },
                      { href: "/finance/fiscal-periods",  label: t("fiscal_periods"),    icon: CalendarDays },
                      { href: "/finance/currencies",      label: t("currencies"),        icon: Coins },
                      { href: "/erp/bank-accounts",       label: t("bank_accounts"),     icon: Landmark },
                    ].map(i => (
                      <div key={i.href} className={!sidebarCollapsed ? "ml-2" : ""}>
                        <NavLink {...i} />
                      </div>
                    ))}
                  </div>
                )}

                {/* Sales */}
                {showSalesGroup && (
                  <div className="mt-1">
                    <SectionLabel text={t("sales")} />
                    {[
                      { href: "/erp/customers",         label: t("customers"),         icon: Users },
                      { href: "/erp/sales-orders",      label: t("sales_orders"),      icon: ShoppingCart },
                      { href: "/erp/deliveries",        label: t("deliveries"),        icon: Truck },
                      { href: "/erp/customer-invoices", label: t("customer_invoices"), icon: Receipt },
                      ...(isFinance ? [
                        { href: "/erp/payments", label: t("payments"), icon: DollarSign },
                        { href: "/erp/cheques",  label: t("cheques"),  icon: CreditCard },
                      ] : []),
                    ].map(i => (
                      <div key={i.href} className={!sidebarCollapsed ? "ml-2" : ""}>
                        <NavLink {...i} />
                      </div>
                    ))}
                  </div>
                )}

                {/* Purchasing */}
                {showPurchasingGroup && (
                  <div className="mt-1">
                    <SectionLabel text={t("purchasing")} />
                    {[
                      { href: "/erp/suppliers",         label: t("suppliers"),         icon: Truck },
                      { href: "/erp/purchase-orders",   label: t("purchase_orders"),   icon: ShoppingBag },
                      { href: "/erp/supplier-invoices", label: t("supplier_invoices"), icon: FileText },
                    ].map(i => (
                      <div key={i.href} className={!sidebarCollapsed ? "ml-2" : ""}>
                        <NavLink {...i} />
                      </div>
                    ))}
                  </div>
                )}

                {/* Inventory */}
                {showInventoryGroup && (
                  <div className="mt-1">
                    <SectionLabel text={t("inventory")} />
                    {[
                      { href: "/erp/products",   label: t("products"),        icon: Package },
                      { href: "/erp/warehouses", label: t("warehouses"),      icon: Warehouse },
                      { href: "/erp/inventory",  label: t("stock_movements"), icon: ArrowLeftRight },
                    ].map(i => (
                      <div key={i.href} className={!sidebarCollapsed ? "ml-2" : ""}>
                        <NavLink {...i} />
                      </div>
                    ))}
                  </div>
                )}
              </>
            )}
          </>
        )}

        {/* ── Bottom items ── */}
        <div className="mt-4 pt-4 border-t border-border space-y-0.5">
          <NavLink href="/reports/sales" label={t("reports")} icon={BarChart3} />
          {/* Users management — Admin only */}
          {isAdmin && (
            <NavLink href="/users/employees" label={t("users")} icon={UserCog} />
          )}
          <NavLink href="/settings/company" label={t("settings")} icon={Settings} />
        </div>
      </nav>

      <button
        onClick={toggleSidebar}
        className="flex items-center gap-2 border-t px-4 py-3 text-sm text-muted-foreground hover:text-foreground shrink-0"
      >
        {sidebarCollapsed
          ? <ChevronsRight className="h-4 w-4" />
          : <ChevronsLeft className="h-4 w-4" />}
        {!sidebarCollapsed && (lang === "ar" ? "طي" : "Collapse")}
      </button>
    </aside>
  );
}
