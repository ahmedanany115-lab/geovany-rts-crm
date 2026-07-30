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
} from "lucide-react";
import { cn } from "@/lib/utils";
import { useUiStore } from "@/stores/ui-state";

interface NavItem {
  label: string;
  href: string;
  icon: React.ComponentType<{ className?: string }>;
}

const navItems: NavItem[] = [
  { label: "Dashboard", href: "/dashboard", icon: LayoutDashboard },
  { label: "CRM", href: "/crm/customers", icon: Users },
  { label: "Quotations", href: "/quotations", icon: FileText },
  { label: "Projects", href: "/projects", icon: Briefcase },
  { label: "Tasks", href: "/tasks/board", icon: KanbanSquare },
  { label: "Help Desk", href: "/helpdesk", icon: LifeBuoy },
  { label: "Inventory", href: "/inventory/products", icon: Package },
  { label: "Invoices", href: "/invoices", icon: Receipt },
  { label: "Reports", href: "/reports/sales", icon: BarChart3 },
  { label: "Users", href: "/users", icon: UserCog },
  { label: "Settings", href: "/settings/company", icon: Settings },
];

export function Sidebar() {
  const pathname = usePathname();
  const { sidebarCollapsed, toggleSidebar } = useUiStore();

  return (
    <aside
      className={cn(
        "flex h-screen flex-col border-r bg-background transition-all",
        sidebarCollapsed ? "w-16" : "w-60"
      )}
    >
      <div className="flex h-14 items-center px-4 font-semibold">
        {sidebarCollapsed ? "R" : "RTS ERP"}
      </div>

      <nav className="flex-1 space-y-1 px-2">
        {navItems.map((item) => {
          const active = pathname?.startsWith(item.href.split("/").slice(0, 2).join("/"));
          const Icon = item.icon;
          return (
            <Link
              key={item.href}
              href={item.href}
              className={cn(
                "flex items-center gap-3 rounded-md px-3 py-2 text-sm transition-colors",
                active
                  ? "bg-accent font-medium text-accent-foreground"
                  : "text-muted-foreground hover:bg-accent/50"
              )}
            >
              <Icon className="h-4 w-4 shrink-0" />
              {!sidebarCollapsed && <span>{item.label}</span>}
            </Link>
          );
        })}
      </nav>

      <button
        onClick={toggleSidebar}
        className="flex items-center gap-2 border-t px-4 py-3 text-sm text-muted-foreground hover:text-foreground"
      >
        {sidebarCollapsed ? <ChevronsRight className="h-4 w-4" /> : <ChevronsLeft className="h-4 w-4" />}
        {!sidebarCollapsed && "Collapse"}
      </button>
    </aside>
  );
}
