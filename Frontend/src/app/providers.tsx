"use client";

import { useEffect, useState } from "react";
import { QueryClientProvider } from "@tanstack/react-query";
import { ThemeProvider } from "next-themes";
import { makeQueryClient } from "@/lib/query-client";
import { authApi } from "@/features/auth/api/authApi";
import { useAuthStore } from "@/stores/auth-store";

function SessionBootstrap() {
  const setSession  = useAuthStore((s) => s.setSession);
  const clearSession = useAuthStore((s) => s.clearSession);

  useEffect(() => {
    // On page load: try a silent refresh.
    // The refresh token is sent as X-Refresh-Token header (if we have one in memory)
    // and as a cookie (if the browser allows cross-origin cookies).
    authApi
      .refresh()
      .then((data) => setSession(data.accessToken, data.user, data.refreshToken))
      .catch(() => clearSession());
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return null;
}

export function Providers({ children }: { children: React.ReactNode }) {
  const [queryClient] = useState(makeQueryClient);

  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider attribute="class" defaultTheme="system" enableSystem>
        <SessionBootstrap />
        {children}
      </ThemeProvider>
    </QueryClientProvider>
  );
}
