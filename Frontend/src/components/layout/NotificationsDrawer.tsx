"use client";

import { useEffect } from "react";
import { X, BellOff, CheckCheck, Trash2 } from "lucide-react";
import { useUiStore } from "@/stores/ui-state";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api-client";
import Link from "next/link";

interface Notif {
  id: string; title: string; body: string; type: string;
  relatedRoute?: string; relatedId?: string;
  isRead: boolean; readAt?: string; createdAt: string;
}

const TYPE_ICON: Record<string, string> = {
  success: "🟢", warning: "🟡", error: "🔴", info: "🔵",
};

function timeAgo(iso: string) {
  const ms = Date.now() - new Date(iso).getTime();
  const m  = Math.floor(ms / 60000);
  const h  = Math.floor(m / 60);
  const d  = Math.floor(h / 24);
  if (d > 0) return `${d}d ago`;
  if (h > 0) return `${h}h ago`;
  if (m > 0) return `${m}m ago`;
  return "just now";
}

export function NotificationsDrawer() {
  const { notificationsOpen, setNotificationsOpen } = useUiStore();
  const qc = useQueryClient();

  const { data, isLoading, refetch } = useQuery<Notif[]>({
    queryKey: ["notifications"],
    queryFn: () => apiFetch("/notifications"),
    refetchInterval: notificationsOpen ? 15000 : 60000,
  });

  useEffect(() => { if (notificationsOpen) refetch(); }, [notificationsOpen, refetch]);

  const markRead = useMutation({
    mutationFn: (id: string) => apiFetch(`/notifications/${id}/read`, { method: "PATCH" }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["notifications"] }),
  });

  const markAll = useMutation({
    mutationFn: () => apiFetch("/notifications/mark-all-read", { method: "PATCH" }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["notifications"] }),
  });

  const remove = useMutation({
    mutationFn: (id: string) => apiFetch(`/notifications/${id}`, { method: "DELETE" }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["notifications"] }),
  });

  if (!notificationsOpen) return null;

  const unread = (data ?? []).filter(n => !n.isRead).length;

  return (
    <>
      <div className="fixed inset-0 z-40 bg-black/20" onClick={() => setNotificationsOpen(false)} />
      <div className="fixed right-0 top-0 z-50 h-screen w-96 border-l bg-background shadow-xl flex flex-col">
        {/* Header */}
        <div className="flex items-center justify-between px-4 py-3 border-b shrink-0">
          <div className="flex items-center gap-2">
            <h2 className="font-semibold">Notifications</h2>
            {unread > 0 && (
              <span className="text-xs bg-primary text-primary-foreground px-1.5 py-0.5 rounded-full font-medium">
                {unread}
              </span>
            )}
          </div>
          <div className="flex items-center gap-1">
            {unread > 0 && (
              <button onClick={() => markAll.mutate()} disabled={markAll.isPending}
                className="p-1.5 rounded hover:bg-accent text-muted-foreground hover:text-foreground text-xs flex items-center gap-1"
                title="Mark all as read">
                <CheckCheck className="h-4 w-4" />
              </button>
            )}
            <button onClick={() => setNotificationsOpen(false)} className="p-1.5 rounded hover:bg-accent text-muted-foreground">
              <X className="h-4 w-4" />
            </button>
          </div>
        </div>

        {/* Body */}
        <div className="flex-1 overflow-y-auto">
          {isLoading && (
            <div className="py-12 text-center text-muted-foreground text-sm">Loading…</div>
          )}
          {!isLoading && (!data || data.length === 0) && (
            <div className="flex flex-col items-center gap-3 py-16 text-center text-muted-foreground">
              <BellOff className="h-10 w-10 opacity-30" />
              <p className="text-sm">You&apos;re all caught up</p>
            </div>
          )}
          {(data ?? []).map(n => (
            <div key={n.id}
              className={`relative group border-b transition-colors ${n.isRead ? "bg-background" : "bg-primary/5"}`}>
              <div className="flex gap-3 px-4 py-3">
                <span className="text-lg shrink-0 pt-0.5">{TYPE_ICON[n.type] ?? "🔵"}</span>
                <div className="flex-1 min-w-0">
                  <p className={`text-sm leading-tight ${n.isRead ? "" : "font-semibold"}`}>{n.title}</p>
                  <p className="text-xs text-muted-foreground mt-0.5 leading-snug">{n.body}</p>
                  <div className="flex items-center gap-2 mt-1.5">
                    <span className="text-xs text-muted-foreground/60">{timeAgo(n.createdAt)}</span>
                    {!n.isRead && (
                      <button onClick={() => markRead.mutate(n.id)}
                        className="text-xs text-primary hover:underline">Mark read</button>
                    )}
                    {n.relatedRoute && (
                      <Link href={n.relatedRoute}
                        onClick={() => { if (!n.isRead) markRead.mutate(n.id); setNotificationsOpen(false); }}
                        className="text-xs text-primary hover:underline">View →</Link>
                    )}
                  </div>
                </div>
                <button onClick={() => remove.mutate(n.id)}
                  className="opacity-0 group-hover:opacity-100 p-1 rounded hover:bg-red-50 text-muted-foreground hover:text-red-500 shrink-0 self-start">
                  <Trash2 className="h-3.5 w-3.5" />
                </button>
              </div>
            </div>
          ))}
        </div>
      </div>
    </>
  );
}
