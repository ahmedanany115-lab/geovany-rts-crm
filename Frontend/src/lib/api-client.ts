import { useAuthStore } from "@/stores/auth-store";

const FALLBACK_API_BASE_URL = "http://localhost:5210/api/v1";
const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL || FALLBACK_API_BASE_URL;

if (!process.env.NEXT_PUBLIC_API_BASE_URL && typeof window !== "undefined") {
  console.warn(
    `[api-client] NEXT_PUBLIC_API_BASE_URL is not set — falling back to ${FALLBACK_API_BASE_URL}.`
  );
}

let refreshInFlight: Promise<boolean> | null = null;

async function tryRefresh(): Promise<boolean> {
  refreshInFlight ??= (async () => {
    try {
      const { refreshToken } = useAuthStore.getState();

      // Send the refresh token both as a cookie (when same-origin allows it)
      // and as a custom header (cross-origin fallback when the cookie is blocked).
      const headers: Record<string, string> = {};
      if (refreshToken) {
        headers["X-Refresh-Token"] = refreshToken;
      }

      const response = await fetch(`${API_BASE_URL}/auth/refresh`, {
        method: "POST",
        credentials: "include",   // still send cookie if the browser will allow it
        headers,
      });

      if (!response.ok) return false;

      const data = await response.json();
      useAuthStore.getState().setSession(data.accessToken, data.user, data.refreshToken);
      return true;
    } catch {
      return false;
    } finally {
      refreshInFlight = null;
    }
  })();

  return refreshInFlight;
}

export async function apiFetch<T>(path: string, init?: RequestInit, isRetry = false): Promise<T> {
  const { accessToken } = useAuthStore.getState();

  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...init,
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}),
      ...init?.headers,
    },
  });

  if (response.status === 401 && !isRetry && path !== "/auth/refresh" && path !== "/auth/login") {
    const refreshed = await tryRefresh();
    if (refreshed) return apiFetch<T>(path, init, true);
    useAuthStore.getState().clearSession();
  }

  if (!response.ok) {
    const problem = await response.json().catch(() => null);
    throw new Error(problem?.title ?? `API error ${response.status}: ${response.statusText}`);
  }

  if (response.status === 204) return undefined as T;
  return response.json() as Promise<T>;
}
