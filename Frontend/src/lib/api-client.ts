import { useAuthStore } from "@/stores/auth-store";

const FALLBACK_API_BASE_URL = "http://localhost:5210/api/v1";
const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL || FALLBACK_API_BASE_URL;

if (!process.env.NEXT_PUBLIC_API_BASE_URL && typeof window !== "undefined") {
  // Loud on purpose: a silently-undefined base URL turns every API call into a request
  // against the Next.js dev server itself, which 404s in a way that looks like an auth bug.
  console.warn(
    `[api-client] NEXT_PUBLIC_API_BASE_URL is not set — falling back to ${FALLBACK_API_BASE_URL}. ` +
    `Create .env.local (see .env.local.example) and restart "npm run dev" to silence this.`
  );
}

let refreshInFlight: Promise<boolean> | null = null;

async function tryRefresh(): Promise<boolean> {
  // Coalesce concurrent 401s into a single refresh call instead of firing one per failed request.
  refreshInFlight ??= (async () => {
    try {
      const response = await fetch(`${API_BASE_URL}/auth/refresh`, {
        method: "POST",
        credentials: "include",
      });
      if (!response.ok) return false;

      const data = await response.json();
      useAuthStore.getState().setSession(data.accessToken, data.user);
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
  const accessToken = useAuthStore.getState().accessToken;

  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...init,
    credentials: "include", // sends the httpOnly refresh cookie
    headers: {
      "Content-Type": "application/json",
      ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}),
      ...init?.headers,
    },
  });

  if (response.status === 401 && !isRetry && path !== "/auth/refresh" && path !== "/auth/login") {
    const refreshed = await tryRefresh();
    if (refreshed) {
      return apiFetch<T>(path, init, true);
    }
    useAuthStore.getState().clearSession();
  }

  if (!response.ok) {
    const problem = await response.json().catch(() => null);
    throw new Error(problem?.title ?? `API error ${response.status}: ${response.statusText}`);
  }

  if (response.status === 204) return undefined as T;
  return response.json() as Promise<T>;
}
