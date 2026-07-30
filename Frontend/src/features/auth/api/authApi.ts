import { apiFetch } from "@/lib/api-client";
import type { AuthResponseDto, LoginRequest, UserDto } from "../types";

export const authApi = {
  login: (payload: LoginRequest) =>
    apiFetch<AuthResponseDto>("/auth/login", {
      method: "POST",
      body: JSON.stringify(payload),
    }),

  refresh: () =>
    apiFetch<AuthResponseDto>("/auth/refresh", { method: "POST" }),

  logout: () => apiFetch<void>("/auth/logout", { method: "POST" }),

  me: () => apiFetch<UserDto>("/auth/me"),
};
