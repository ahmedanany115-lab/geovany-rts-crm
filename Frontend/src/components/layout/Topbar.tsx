"use client";

import { useState } from "react";
import Link from "next/link";
import { Bell, Search, LogOut, User as UserIcon, Languages } from "lucide-react";
import { useUiStore } from "@/stores/ui-state";
import { useCurrentUser, useLogout } from "@/features/auth/hooks/useAuth";
import { ThemeToggle } from "./ThemeToggle";

export function Topbar() {
  const toggleNotifications = useUiStore((s) => s.toggleNotifications);
  const { language, toggleLanguage } = useUiStore();
  const user = useCurrentUser();
  const logout = useLogout();
  const [menuOpen, setMenuOpen] = useState(false);

  const initials = user
    ? `${user.firstName?.[0] ?? ""}${user.lastName?.[0] ?? ""}`.toUpperCase() || "U"
    : "?";

  return (
    <header className="flex h-14 items-center justify-between border-b px-4">
      <button className="flex w-72 items-center gap-2 rounded-md border px-3 py-1.5 text-sm text-muted-foreground">
        <Search className="h-4 w-4" />
        Search…
        <kbd className="ml-auto rounded border px-1.5 text-xs">⌘K</kbd>
      </button>

      <div className="flex items-center gap-1">
        {/* Language switcher */}
        <button
          onClick={toggleLanguage}
          title={language === "en" ? "Switch to Arabic" : "التبديل إلى الإنجليزية"}
          className="flex h-9 items-center gap-1.5 rounded-md px-2.5 text-xs font-semibold text-muted-foreground hover:bg-accent hover:text-foreground transition-colors"
          aria-label="Toggle language"
        >
          <Languages className="h-4 w-4" />
          {language === "en" ? "عربي" : "EN"}
        </button>

        <button
          onClick={toggleNotifications}
          className="relative flex h-9 w-9 items-center justify-center rounded-md text-muted-foreground hover:bg-accent hover:text-foreground"
          aria-label="Notifications"
        >
          <Bell className="h-4 w-4" />
        </button>

        <ThemeToggle />

        <div className="relative ml-2">
          <button
            onClick={() => setMenuOpen((v) => !v)}
            className="flex h-9 w-9 items-center justify-center rounded-full bg-accent text-xs font-medium"
          >
            {initials}
          </button>

          {menuOpen && (
            <div className="absolute right-0 top-11 w-44 rounded-md border bg-background py-1 shadow-lg z-50">
              <Link
                href="/profile"
                className="flex items-center gap-2 px-3 py-2 text-sm hover:bg-accent"
                onClick={() => setMenuOpen(false)}
              >
                <UserIcon className="h-4 w-4" /> Profile
              </Link>
              <button
                onClick={() => logout.mutate()}
                className="flex w-full items-center gap-2 px-3 py-2 text-left text-sm text-red-600 hover:bg-accent"
              >
                <LogOut className="h-4 w-4" /> Log out
              </button>
            </div>
          )}
        </div>
      </div>
    </header>
  );
}
