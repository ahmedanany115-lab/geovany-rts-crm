import { create } from "zustand";

interface UiState {
  sidebarCollapsed: boolean;
  notificationsOpen: boolean;
  toggleSidebar: () => void;
  toggleNotifications: () => void;
  setNotificationsOpen: (open: boolean) => void;
}

export const useUiStore = create<UiState>((set) => ({
  sidebarCollapsed: false,
  notificationsOpen: false,
  toggleSidebar: () => set((s) => ({ sidebarCollapsed: !s.sidebarCollapsed })),
  toggleNotifications: () => set((s) => ({ notificationsOpen: !s.notificationsOpen })),
  setNotificationsOpen: (open) => set({ notificationsOpen: open }),
}));
