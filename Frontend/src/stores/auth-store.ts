import { create } from "zustand";
import type { UserDto } from "@/features/auth/types";

interface AuthState {
  accessToken: string | null;
  user: UserDto | null;
  isInitializing: boolean; // true until the first silent-refresh attempt on app load resolves
  setSession: (accessToken: string, user: UserDto) => void;
  clearSession: () => void;
  setInitializing: (value: boolean) => void;
}

// Deliberately in-memory only (Zustand's default, no persist middleware) — the access
// token must never touch localStorage/sessionStorage, per Architecture.md §5.
export const useAuthStore = create<AuthState>((set) => ({
  accessToken: null,
  user: null,
  isInitializing: true,
  setSession: (accessToken, user) => set({ accessToken, user, isInitializing: false }),
  clearSession: () => set({ accessToken: null, user: null, isInitializing: false }),
  setInitializing: (value) => set({ isInitializing: value }),
}));

export function hasPermission(code: string): boolean {
  return useAuthStore.getState().user?.permissions.includes(code) ?? false;
}
