"use client";

import Image from "next/image";
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
                "flex h-screen flex-col border-r border-zinc-800 bg-zinc-950 text-white transition-all duration-300",
                sidebarCollapsed ? "w-20" : "w-72"
            )}
        >
            {/* Logo */}
            <div className="flex items-center gap-3 border-b border-zinc-800 px-5 py-5">
                <Image
                    src="/logo.png"
                    alt="Royal Technology System"
                    width={48}
                    height={48}
                    className="rounded-lg"
                />

                {!sidebarCollapsed && (
                    <div>
                        <h2 className="text-lg font-bold tracking-wide">
                            Royal CRM
                        </h2>
                        <p className="text-xs text-zinc-400">
                            Royal Technology System
                        </p>
                    </div>
                )}
            </div>

            {/* Navigation */}
            <nav className="flex-1 space-y-2 px-3 py-5">
                {navItems.map((item) => {
                    const active =
                        pathname?.startsWith(item.href.split("/").slice(0, 2).join("/"));

                    const Icon = item.icon;

                    return (
                        <Link
                            key={item.href}
                            href={item.href}
                            className={cn(
                                "flex items-center gap-3 rounded-xl px-4 py-3 text-sm font-medium transition-all duration-200",
                                active
                                    ? "bg-blue-600 text-white shadow-lg"
                                    : "text-zinc-400 hover:bg-zinc-800 hover:text-white"
                            )}
                        >
                            <Icon className="h-5 w-5 shrink-0" />

                            {!sidebarCollapsed && (
                                <span>{item.label}</span>
                            )}
                        </Link>
                    );
                })}
            </nav>

            {/* Collapse */}
            <button
                onClick={toggleSidebar}
                className="flex items-center gap-2 border-t border-zinc-800 px-5 py-4 text-sm text-zinc-400 transition hover:bg-zinc-900 hover:text-white"
            >
                {sidebarCollapsed ? (
                    <ChevronsRight className="h-5 w-5" />
                ) : (
                    <ChevronsLeft className="h-5 w-5" />
                )}

                {!sidebarCollapsed && <span>Collapse Menu</span>}
            </button>
        </aside>
    );
}