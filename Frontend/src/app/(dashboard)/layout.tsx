"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { Topbar } from "@/components/layout/Topbar";
import { ModuleNav } from "@/components/layout/ModuleNav";
import { NotificationsDrawer } from "@/components/layout/NotificationsDrawer";
import { useAuthStore } from "@/stores/auth-store";

export default function DashboardLayout({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const { user, isInitializing } = useAuthStore();

  useEffect(() => {
    if (!isInitializing && !user) {
      router.replace("/login");
    }
  }, [isInitializing, user, router]);

  if (isInitializing) {
    return (
      <div className="flex h-screen items-center justify-center text-sm text-muted-foreground">
        Loading…
      </div>
    );
  }

  if (!user) return null; // redirect effect above is already firing

  return (
    <div className="flex h-screen flex-col overflow-hidden bg-muted/10">
      <Topbar />
      <ModuleNav />
      <main className="flex-1 overflow-y-auto">
        <div className="mx-auto w-full max-w-[1600px] p-4 md:p-6">{children}</div>
      </main>
      <NotificationsDrawer />
    </div>
  );
}
