"use client";

import { useState, useEffect, useRef, useCallback } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { Bell, Search, LogOut, User as UserIcon, Languages, X } from "lucide-react";
import { useUiStore } from "@/stores/ui-state";
import { useCurrentUser, useLogout } from "@/features/auth/hooks/useAuth";
import { ThemeToggle } from "./ThemeToggle";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api-client";

const TYPE_ICON: Record<string, string> = {
  Customer: "👤", Supplier: "🏭", "Sales Order": "📦",
  Invoice: "🧾", Product: "📦", Contract: "🔧",
  "Purchase Order": "🛒", Document: "📄",
};

export function Topbar() {
  const toggleNotifications = useUiStore((s) => s.toggleNotifications);
  const { language, toggleLanguage } = useUiStore();
  const user    = useCurrentUser();
  const logout  = useLogout();
  const router  = useRouter();
  const qc      = useQueryClient();

  const [menuOpen,    setMenuOpen]    = useState(false);
  const [searchOpen,  setSearchOpen]  = useState(false);
  const [searchTerm,  setSearchTerm]  = useState("");
  const [debouncedQ,  setDebouncedQ]  = useState("");
  const searchRef  = useRef<HTMLDivElement>(null);
  const inputRef   = useRef<HTMLInputElement>(null);

  // Debounce
  useEffect(() => {
    const t = setTimeout(() => setDebouncedQ(searchTerm), 280);
    return () => clearTimeout(t);
  }, [searchTerm]);

  // Search results
  const { data: searchData, isLoading: searchLoading } = useQuery({
    queryKey: ["search", debouncedQ],
    queryFn: () => apiFetch<{ results: { id: string; name: string; code?: string; type: string; href: string }[] }>(`/search?q=${encodeURIComponent(debouncedQ)}`),
    enabled: debouncedQ.length >= 2,
    staleTime: 10000,
  });

  // Unread notification count
  const { data: bellData } = useQuery({
    queryKey: ["notifications-count"],
    queryFn: () => apiFetch<{ count: number }>("/notifications/unread-count"),
    refetchInterval: 30000,
  });
  const unreadCount = (bellData as any)?.count ?? 0;

  // Keyboard shortcut ⌘K / Ctrl+K
  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && e.key === "k") {
        e.preventDefault(); setSearchOpen(true); setTimeout(() => inputRef.current?.focus(), 50);
      }
      if (e.key === "Escape") { setSearchOpen(false); setSearchTerm(""); }
    };
    window.addEventListener("keydown", handler);
    return () => window.removeEventListener("keydown", handler);
  }, []);

  // Click outside to close search
  useEffect(() => {
    const h = (e: MouseEvent) => { if (searchRef.current && !searchRef.current.contains(e.target as Node)) { setSearchOpen(false); setSearchTerm(""); } };
    if (searchOpen) document.addEventListener("mousedown", h);
    return () => document.removeEventListener("mousedown", h);
  }, [searchOpen]);

  const handleSearchSelect = useCallback((href: string) => {
    router.push(href); setSearchOpen(false); setSearchTerm("");
  }, [router]);

  // Group results by type
  const grouped: Record<string, { id: string; name: string; code?: string; type: string; href: string }[]> = {};
  ((searchData as any)?.results ?? []).forEach((r: any) => {
    if (!grouped[r.type]) grouped[r.type] = [];
    grouped[r.type].push(r);
  });

  const initials = user
    ? `${user.firstName?.[0] ?? ""}${user.lastName?.[0] ?? ""}`.toUpperCase() || "U"
    : "?";

  return (
    <header className="flex h-14 items-center justify-between border-b px-4 relative">
      {/* Search bar */}
      <div ref={searchRef} className="relative w-72">
        <button
          onClick={() => { setSearchOpen(true); setTimeout(() => inputRef.current?.focus(), 50); }}
          className="flex w-full items-center gap-2 rounded-md border px-3 py-1.5 text-sm text-muted-foreground hover:border-primary/50 transition-colors"
        >
          <Search className="h-4 w-4 shrink-0" />
          {searchOpen
            ? <input
                ref={inputRef}
                value={searchTerm}
                onChange={e => setSearchTerm(e.target.value)}
                placeholder="Search customers, orders, products…"
                className="flex-1 bg-transparent outline-none text-foreground placeholder:text-muted-foreground"
                autoFocus
              />
            : <span className="flex-1 text-left">Search…</span>
          }
          <kbd className="ml-auto rounded border px-1.5 text-xs shrink-0">⌘K</kbd>
        </button>

        {/* Search dropdown */}
        {searchOpen && (searchTerm.length >= 2) && (
          <div className="absolute top-full left-0 right-0 mt-1 rounded-xl border bg-background shadow-2xl z-[200] overflow-hidden max-h-96 overflow-y-auto">
            {searchLoading && (
              <div className="p-4 text-sm text-muted-foreground">Searching…</div>
            )}
            {!searchLoading && Object.keys(grouped).length === 0 && debouncedQ.length >= 2 && (
              <div className="p-4 text-sm text-muted-foreground">No results for "{debouncedQ}"</div>
            )}
            {Object.entries(grouped).map(([type, items]) => (
              <div key={type}>
                <div className="px-3 py-1.5 text-xs font-bold uppercase tracking-widest text-muted-foreground/50 bg-muted/30">
                  {type}
                </div>
                {items.map(r => (
                  <button key={r.id} onClick={() => handleSearchSelect(r.href)}
                    className="w-full flex items-center gap-3 px-3 py-2 text-sm hover:bg-accent text-left transition-colors">
                    <span className="shrink-0">{TYPE_ICON[type] ?? "📄"}</span>
                    <div className="min-w-0 flex-1">
                      <p className="font-medium truncate">{r.name}</p>
                      {r.code && <p className="text-xs text-muted-foreground truncate">{r.code}</p>}
                    </div>
                    <span className="text-xs text-muted-foreground shrink-0">{type}</span>
                  </button>
                ))}
              </div>
            ))}
          </div>
        )}
      </div>

      <div className="flex items-center gap-1">
        {/* Language */}
        <button
          onClick={toggleLanguage}
          title={language === "en" ? "Switch to Arabic" : "التبديل إلى الإنجليزية"}
          className="flex h-9 items-center gap-1.5 rounded-md px-2.5 text-xs font-semibold text-muted-foreground hover:bg-accent hover:text-foreground transition-colors"
        >
          <Languages className="h-4 w-4" />
          {language === "en" ? "عربي" : "EN"}
        </button>

        {/* Bell */}
        <button
          onClick={() => { toggleNotifications(); qc.invalidateQueries({ queryKey: ["notifications"] }); }}
          className="relative flex h-9 w-9 items-center justify-center rounded-md text-muted-foreground hover:bg-accent hover:text-foreground"
          aria-label="Notifications"
        >
          <Bell className="h-4 w-4" />
          {unreadCount > 0 && (
            <span className="absolute -top-0.5 -right-0.5 flex h-4 w-4 items-center justify-center rounded-full bg-red-500 text-[10px] font-bold text-white leading-none">
              {unreadCount > 9 ? "9+" : unreadCount}
            </span>
          )}
        </button>

        <ThemeToggle />

        {/* Avatar menu */}
        <div className="relative ml-2">
          <button
            onClick={() => setMenuOpen((v) => !v)}
            className="flex h-9 w-9 items-center justify-center rounded-full bg-primary/10 text-xs font-bold text-primary ring-2 ring-primary/20 hover:ring-primary/40 transition-all"
          >
            {initials}
          </button>
          {menuOpen && (
            <div className="absolute right-0 top-11 w-48 rounded-xl border bg-background py-1 shadow-xl z-50">
              <div className="px-3 py-2 border-b">
                <p className="text-sm font-semibold">{user?.firstName} {user?.lastName}</p>
                <p className="text-xs text-muted-foreground truncate">{user?.email}</p>
              </div>
              <Link href="/profile" className="flex items-center gap-2 px-3 py-2 text-sm hover:bg-accent" onClick={() => setMenuOpen(false)}>
                <UserIcon className="h-4 w-4" /> Profile
              </Link>
              <button onClick={() => logout.mutate()} className="flex w-full items-center gap-2 px-3 py-2 text-left text-sm text-red-600 hover:bg-accent">
                <LogOut className="h-4 w-4" /> Log out
              </button>
            </div>
          )}
        </div>
      </div>
    </header>
  );
}
