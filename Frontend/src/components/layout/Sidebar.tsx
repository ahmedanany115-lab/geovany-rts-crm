"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  LayoutDashboard,
  Users,
  FileText,
  Briefcase,
  KanbanSquare,
  LifeBuoy,
  Package,
  Receipt,
  BarChart3,
  UserCog,
  Settings,
  ChevronsLeft,
  ChevronsRight,
  Landmark,
  ShoppingCart,
  ShoppingBag,
  Truck,
  CreditCard,
  DollarSign,
  Warehouse,
  TrendingUp,
  BookOpen,
  ArrowLeftRight,
} from "lucide-react";
import { cn } from "@/lib/utils";
import { useUiStore } from "@/stores/ui-state";
import { useState } from "react";

interface NavItem {
  label: string;
  href: string;
  icon: React.ComponentType<{ className?: string }>;
}

interface NavGroup {
  group: string;
  items: NavItem[];
}

const topItems: NavItem[] = [
  { label: "Dashboard", href: "/dashboard", icon: LayoutDashboard },
  { label: "CRM", href: "/crm/customers", icon: Users },
  { label: "Projects", href: "/projects", icon: Briefcase },
  { label: "Tasks", href: "/tasks/board", icon: KanbanSquare },
  { label: "Help Desk", href: "/helpdesk", icon: LifeBuoy },
];

const erpGroups: NavGroup[] = [
  {
    group: "ERP Overview",
    items: [
      { label: "ERP Dashboard", href: "/erp/dashboard", icon: TrendingUp },
    ],
  },
  {
    group: "Finance",
    items: [
      { label: "Chart of Accounts", href: "/finance/accounts", icon: BookOpen },
      { label: "Journal Entries", href: "/finance/journal-entries", icon: FileText },
      { label: "Ledger", href: "/finance/ledger", icon: Receipt },
      { label: "Trial Balance", href: "/finance/trial-balance", icon: BarChart3 },
      { label: "Bank Accounts", href: "/erp/bank-accounts", icon: Landmark },
    ],
  },
  {
    group: "Sales",
    items: [
      { label: "Customers", href: "/erp/customers", icon: Users },
      { label: "Sales Orders", href: "/erp/sales-orders", icon: ShoppingCart },
      { label: "Customer Invoices", href: "/erp/customer-invoices", icon: Receipt },
      { label: "Payments", href: "/erp/payments", icon: DollarSign },
      { label: "Cheques", href: "/erp/cheques", icon: CreditCard },
    ],
  },
  {
    group: "Purchasing",
    items: [
      { label: "Suppliers", href: "/erp/suppliers", icon: Truck },
      { label: "Purchase Orders", href: "/erp/purchase-orders", icon: ShoppingBag },
      { label: "Supplier Invoices", href: "/erp/supplier-invoices", icon: FileText },
    ],
  },
  {
    group: "Inventory",
    items: [
      { label: "Products", href: "/erp/products", icon: Package },
      { label: "Warehouses", href: "/erp/warehouses", icon: Warehouse },
      { label: "Stock & Movements", href: "/erp/inventory", icon: ArrowLeftRight },
    ],
  },
];

export function Sidebar() {
  const pathname = usePathname();
  const { sidebarCollapsed, toggleSidebar } = useUiStore();
  const [erpExpanded, setErpExpanded] = useState(pathname?.startsWith("/erp") || pathname?.startsWith("/finance"));

  const isActive = (href: string) => pathname === href || pathname?.startsWith(href + "/");

  const NavLink = ({ item }: { item: NavItem }) => {
    const active = isActive(item.href);
    const Icon = item.icon;
    return (
      <Link href={item.href}
        className={cn(
          "flex items-center gap-3 rounded-md px-3 py-2 text-sm transition-colors",
          active ? "bg-accent font-medium text-accent-foreground" : "text-muted-foreground hover:bg-accent/50"
        )}>
        <Icon className="h-4 w-4 shrink-0" />
        {!sidebarCollapsed && <span>{item.label}</span>}
      </Link>
    );
  };

  return (
    <aside className={cn("flex h-screen flex-col border-r bg-background transition-all duration-200", sidebarCollapsed ? "w-16" : "w-60")}>
      <div className="flex h-14 items-center gap-2 px-4 font-semibold text-primary shrink-0">
        <div className="flex h-7 w-7 items-center justify-center rounded-md bg-primary text-sm font-bold text-primary-foreground shrink-0">R</div>
        {!sidebarCollapsed && <span>Royal ERP</span>}
      </div>

      <nav className="flex-1 overflow-y-auto px-2 py-1 space-y-0.5">
        {/* Top items */}
        {topItems.map(item => <NavLink key={item.href} item={item} />)}

        {/* ERP section toggle */}
        <button
          onClick={() => setErpExpanded(v => !v)}
          className={cn(
            "w-full flex items-center gap-3 rounded-md px-3 py-2 text-sm transition-colors mt-2",
            erpExpanded ? "text-primary font-medium" : "text-muted-foreground hover:bg-accent/50"
          )}>
          <TrendingUp className="h-4 w-4 shrink-0" />
          {!sidebarCollapsed && (
            <>
              <span className="flex-1 text-left">Finance & ERP</span>
              <span className="text-xs opacity-60">{erpExpanded ? "▲" : "▼"}</span>
            </>
          )}
        </button>

        {erpExpanded && erpGroups.map(g => (
          <div key={g.group} className="mt-1">
            {!sidebarCollapsed && (
              <div className="px-3 py-1 text-xs font-semibold uppercase tracking-wider text-muted-foreground/60">
                {g.group}
              </div>
            )}
            {g.items.map(item => (
              <div key={item.href} className={!sidebarCollapsed ? "ml-2" : ""}>
                <NavLink item={item} />
              </div>
            ))}
          </div>
        ))}

        {/* Bottom items */}
        <div className="mt-4 pt-4 border-t border-border space-y-0.5">
          <NavLink item={{ label: "Reports", href: "/reports/sales", icon: BarChart3 }} />
          <NavLink item={{ label: "Users", href: "/users", icon: UserCog }} />
          <NavLink item={{ label: "Settings", href: "/settings/company", icon: Settings }} />
        </div>
      </nav>

      <button
        onClick={toggleSidebar}
        className="flex items-center gap-2 border-t px-4 py-3 text-sm text-muted-foreground hover:text-foreground shrink-0"
      >
        {sidebarCollapsed ? <ChevronsRight className="h-4 w-4" /> : <ChevronsLeft className="h-4 w-4" />}
        {!sidebarCollapsed && "Collapse"}
      </button>
    </aside>
  );
}
