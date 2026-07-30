"use client";

import { X, BellOff } from "lucide-react";
import { useUiStore } from "@/stores/ui-state";

export function NotificationsDrawer() {
  const { notificationsOpen, setNotificationsOpen } = useUiStore();

  if (!notificationsOpen) return null;

  return (
    <>
      <div
        className="fixed inset-0 z-40 bg-black/20"
        onClick={() => setNotificationsOpen(false)}
      />
      <div className="fixed right-0 top-0 z-50 h-screen w-80 border-l bg-background p-4 shadow-lg">
        <div className="mb-4 flex items-center justify-between">
          <h2 className="font-semibold">Notifications</h2>
          <button onClick={() => setNotificationsOpen(false)} aria-label="Close">
            <X className="h-4 w-4" />
          </button>
        </div>

        {/* GET /notifications wiring lands with the Notifications module (see SolutionArchitecture.md §4) */}
        <div className="flex flex-col items-center justify-center gap-2 py-16 text-center text-muted-foreground">
          <BellOff className="h-8 w-8" />
          <p className="text-sm">You&apos;re all caught up</p>
        </div>
      </div>
    </>
  );
}
