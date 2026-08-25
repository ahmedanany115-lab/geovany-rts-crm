import { create } from "zustand";
import type { UserDto } from "@/features/auth/types";

interface AuthState {
  accessToken: string | null;
  refreshToken: string | null;   // stored in memory, sent as header for cross-origin refresh
  user: UserDto | null;
  isInitializing: boolean;
  setSession: (accessToken: string, user: UserDto, refreshToken?: string) => void;
  clearSession: () => void;
  setInitializing: (value: boolean) => void;
}

export const useAuthStore = create<AuthState>((set) => ({
  accessToken: null,
  refreshToken: null,
  user: null,
  isInitializing: true,
  setSession: (accessToken, user, refreshToken) =>
    set({ accessToken, user, refreshToken: refreshToken ?? null, isInitializing: false }),
  clearSession: () =>
    set({ accessToken: null, refreshToken: null, user: null, isInitializing: false }),
  setInitializing: (value) => set({ isInitializing: value }),
}));

export function hasPermission(code: string): boolean {
  return useAuthStore.getState().user?.permissions.includes(code) ?? false;
}
