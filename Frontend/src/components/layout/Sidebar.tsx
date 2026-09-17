"use client";

import Image from "next/image";
import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  LayoutDashboard, Users, FileText, Briefcase, KanbanSquare, LifeBuoy,
  Package, Receipt, BarChart3, UserCog, Settings, ChevronsLeft, ChevronsRight,
  Landmark, ShoppingCart, ShoppingBag, Truck, CreditCard, DollarSign,
  Warehouse, TrendingUp, BookOpen, ArrowLeftRight, CalendarDays, Coins,
  HeartHandshake, Wrench, ClipboardList, HardDrive, Calculator, FileCheck2,
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

  const [financeOpen, setFinanceOpen]     = useState(pathname?.startsWith("/finance") ?? false);
  const [erpOpen, setErpOpen]             = useState(pathname?.startsWith("/erp") ?? false);
  const [maintenanceOpen, setMaintenanceOpen] = useState(pathname?.startsWith("/erp/maintenance") ?? false);

  const isActive = (href: string) =>
    pathname === href || pathname?.startsWith(href + "/");

  const NavLink = ({
    href, label, icon: Icon, indent = false,
  }: { href: string; label: string; icon: React.ComponentType<{ className?: string }>; indent?: boolean }) => {
    const active = isActive(href);
    return (
      <Link
        href={href}
        className={cn(
          "flex items-center gap-3 rounded-md px-3 py-2 text-sm transition-colors",
          indent && !sidebarCollapsed && "ml-3",
          active
            ? "bg-accent font-semibold text-accent-foreground"
            : "text-muted-foreground hover:bg-accent/50 hover:text-foreground"
        )}
      >
        <Icon className="h-4 w-4 shrink-0" />
        {!sidebarCollapsed && <span>{label}</span>}
      </Link>
    );
  };

  const Collapsible = ({
    open, onToggle, label, icon: Icon, children,
  }: {
    open: boolean; onToggle: () => void;
    label: string; icon: React.ComponentType<{ className?: string }>;
    children: React.ReactNode;
  }) => (
    <>
      <button
        onClick={onToggle}
        className={cn(
          "w-full flex items-center gap-3 rounded-md px-3 py-2 text-sm transition-colors",
          open ? "text-foreground font-semibold" : "text-muted-foreground hover:bg-accent/50"
        )}
      >
        <Icon className="h-4 w-4 shrink-0" />
        {!sidebarCollapsed && (
          <>
            <span className="flex-1 text-left">{label}</span>
            <span className="text-xs opacity-50">{open ? "▲" : "▼"}</span>
          </>
        )}
      </button>
      {open && children}
    </>
  );

  const Divider = ({ label }: { label?: string }) =>
    !sidebarCollapsed && label ? (
      <div className="px-3 pt-3 pb-0.5 text-xs font-bold uppercase tracking-widest text-muted-foreground/50">
        {label}
      </div>
    ) : <div className="my-1 border-t border-border/40" />;

  // Role flags
  const showFinance     = isFinance;
  const showErp         = isFinance || hasRole("Manager", "Sales", "Purchasing", "Delivery", "SupportAgent", "Marketing", "ReadOnly");
  const showSales       = hasRole("Admin", "Manager", "SalesManager", "Sales", "Accountant", "Delivery");
  const showPurchasing  = hasRole("Admin", "Manager", "SalesManager", "Accountant", "Purchasing");
  const showInventory   = hasRole("Admin", "Manager", "SalesManager", "Sales", "Accountant", "Purchasing", "Delivery");
  const showMaintenance = showErp;

  return (
    <aside className={cn(
      "flex h-screen flex-col border-r bg-background transition-all duration-200",
      sidebarCollapsed ? "w-16" : "w-64",
      lang === "ar" ? "border-l border-r-0" : ""
    )}>
      {/* Logo */}
      <div className="flex h-16 items-center gap-3 px-3 shrink-0">
        <div className={`relative flex items-center justify-center shrink-0 ${sidebarCollapsed ? "w-10 h-10" : "w-10 h-10"}`}>
          <Image
            src="/logo.png"
            alt="Royal Technology System"
            width={40}
            height={40}
            className="object-contain w-full h-full"
            priority
          />
        </div>
        {!sidebarCollapsed && (
          <div className="min-w-0">
            <p className="text-sm font-bold text-primary leading-tight truncate">Royal Technology</p>
            <p className="text-xs text-muted-foreground leading-tight truncate">System</p>
          </div>
        )}
      </div>

      <nav className="flex-1 overflow-y-auto px-2 py-2 space-y-0.5">
        {/* ── General ── */}
        <NavLink href="/dashboard"     label={t("dashboard")} icon={LayoutDashboard} />
        <NavLink href="/projects"      label={t("projects")}  icon={Briefcase} />
        <NavLink href="/tasks/board"   label={t("tasks")}     icon={KanbanSquare} />
        <NavLink href="/helpdesk"      label={t("helpdesk")}  icon={LifeBuoy} />

        {/* ── CRM ── */}
        <Divider label="CRM" />
        <NavLink href="/crm/customers" label={t("customers")} icon={Users} />
        <NavLink href="/crm/leads"     label={t("leads")}     icon={TrendingUp} />
        <NavLink href="/quotations"    label={t("quotations")} icon={FileCheck2} />

        {/* ── Self Service ── */}
        <Divider label={t("self_service")} />
        <NavLink href="/hr/leaves"   label={t("leaves")}   icon={HeartHandshake} />
        <NavLink href="/hr/meetings" label={t("meetings")} icon={CalendarDays} />

        {/* ── Finance (standalone, RBAC-gated) ── */}
        {showFinance && (
          <>
            <Divider label={t("finance")} />
            <Collapsible
              open={financeOpen}
              onToggle={() => setFinanceOpen(v => !v)}
              label={t("finance")}
              icon={Calculator}
            >
              {[
                { href: "/finance/accounts",        label: t("chart_of_accounts"), icon: BookOpen },
                { href: "/finance/journal-entries", label: t("journal_entries"),   icon: FileText },
                { href: "/finance/ledger",           label: t("ledger"),            icon: Receipt },
                { href: "/finance/trial-balance",    label: t("trial_balance"),     icon: BarChart3 },
                { href: "/finance/income-statement", label: lang === "ar" ? "قائمة الدخل" : "Income Statement", icon: TrendingUp },
                { href: "/finance/profit-loss",      label: lang === "ar" ? "الأرباح والخسائر" : "Profit & Loss",  icon: BarChart3 },
                { href: "/finance/balance-sheet",    label: lang === "ar" ? "الميزانية العمومية" : "Balance Sheet", icon: BarChart3 },
                { href: "/finance/fiscal-periods",   label: t("fiscal_periods"),    icon: CalendarDays },
                { href: "/finance/currencies",       label: t("currencies"),        icon: Coins },
                { href: "/erp/bank-accounts",        label: t("bank_accounts"),     icon: Landmark },
                { href: "/erp/customer-invoices",    label: t("customer_invoices"), icon: Receipt },
                { href: "/erp/supplier-invoices",    label: lang === "ar" ? "فواتير الموردين" : "Supplier Invoices", icon: FileText },
              ].map(i => <NavLink key={i.href} {...i} indent />)}
            </Collapsible>
          </>
        )}

        {/* ── ERP Operations ── */}
        {showErp && (
          <>
            <Divider label="ERP" />
            <NavLink href="/erp/dashboard" label={t("erp_dashboard")} icon={TrendingUp} />

            {/* Sales sub-group */}
            {showSales && (
              <Collapsible
                open={erpOpen}
                onToggle={() => setErpOpen(v => !v)}
                label={t("sales")}
                icon={ShoppingCart}
              >
                {[
                  { href: "/erp/customers",         label: t("customers"),         icon: Users },
                  { href: "/erp/sales-orders",      label: t("sales_orders"),      icon: ShoppingCart },
                  { href: "/erp/deliveries",         label: t("deliveries"),        icon: Truck },
                  { href: "/erp/customer-invoices", label: t("customer_invoices"), icon: Receipt },
                  ...(isFinance ? [
                    { href: "/erp/payments", label: t("payments"), icon: DollarSign },
                    { href: "/erp/cheques",  label: t("cheques"),  icon: CreditCard },
                  ] : []),
                ].map(i => <NavLink key={i.href} {...i} indent />)}
              </Collapsible>
            )}

            {/* Purchasing */}
            {showPurchasing && (
              <>
                {[
                  { href: "/erp/suppliers",         label: t("suppliers"),         icon: Truck },
                  { href: "/erp/purchase-orders",   label: t("purchase_orders"),   icon: ShoppingBag },
                  { href: "/erp/supplier-invoices", label: t("supplier_invoices"), icon: FileText },
                ].map(i => <NavLink key={i.href} {...i} />)}
              </>
            )}

            {/* Inventory */}
            {showInventory && (
              <>
                {[
                  { href: "/erp/products",   label: t("products"),        icon: Package },
                  { href: "/erp/warehouses", label: t("warehouses"),      icon: Warehouse },
                  { href: "/erp/inventory",  label: t("stock_movements"), icon: ArrowLeftRight },
                ].map(i => <NavLink key={i.href} {...i} />)}
              </>
            )}
          </>
        )}

        {/* ── Maintenance ── */}
        {showMaintenance && (
          <>
            <Divider label={t("maintenance")} />
            <Collapsible
              open={maintenanceOpen}
              onToggle={() => setMaintenanceOpen(v => !v)}
              label={t("maintenance")}
              icon={Wrench}
            >
              {[
                { href: "/erp/maintenance",           label: t("maintenance"),           icon: Wrench },
                { href: "/erp/maintenance/contracts", label: t("maintenance_contracts"), icon: ClipboardList },
                { href: "/erp/maintenance/visits",    label: t("maintenance_visits"),    icon: CalendarDays },
                { href: "/erp/maintenance/equipment", label: t("maintenance_equipment"), icon: HardDrive },
              ].map(i => <NavLink key={i.href} {...i} indent />)}
            </Collapsible>
          </>
        )}

        {/* ── Bottom ── */}
        <Divider />
        <NavLink href="/reports/sales"    label={t("reports")}   icon={BarChart3} />
        <NavLink href="/documents"        label={lang === "ar" ? "وثائق الشركة" : "Documents"} icon={FileText} />
        {isAdmin && <NavLink href="/admin/user-activity" label={lang === "ar" ? "نشاط المستخدمين" : "User Activity"} icon={BarChart3} />}
        {isAdmin && <NavLink href="/users/employees" label={t("users")} icon={UserCog} />}
        <NavLink href="/settings/company" label={t("settings")} icon={Settings} />
      </nav>

      <button
        onClick={toggleSidebar}
        className="flex items-center gap-2 border-t px-4 py-3 text-sm text-muted-foreground hover:text-foreground shrink-0"
      >
        {sidebarCollapsed ? <ChevronsRight className="h-4 w-4" /> : <ChevronsLeft className="h-4 w-4" />}
        {!sidebarCollapsed && (lang === "ar" ? "طي" : "Collapse")}
      </button>
    </aside>
  );
}
