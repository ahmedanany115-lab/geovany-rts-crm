import { create } from "zustand";

interface UiState {
  sidebarCollapsed: boolean;
  notificationsOpen: boolean;
  language: "en" | "ar";
  toggleSidebar: () => void;
  toggleNotifications: () => void;
  setNotificationsOpen: (open: boolean) => void;
  toggleLanguage: () => void;
}

export const useUiStore = create<UiState>((set) => ({
  sidebarCollapsed: false,
  notificationsOpen: false,
  language: "en",
  toggleSidebar: () => set((s) => ({ sidebarCollapsed: !s.sidebarCollapsed })),
  toggleNotifications: () => set((s) => ({ notificationsOpen: !s.notificationsOpen })),
  setNotificationsOpen: (open) => set({ notificationsOpen: open }),
  toggleLanguage: () =>
    set((s) => {
      const next = s.language === "en" ? "ar" : "en";
      // Apply RTL direction + lang attribute on the document root
      if (typeof document !== "undefined") {
        document.documentElement.lang = next;
        document.documentElement.dir  = next === "ar" ? "rtl" : "ltr";
      }
      return { language: next };
    }),
}));
