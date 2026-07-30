"use client";

import { useEffect, useState } from "react";
import { QueryClientProvider } from "@tanstack/react-query";
import { ThemeProvider } from "next-themes";
import { makeQueryClient } from "@/lib/query-client";
import { authApi } from "@/features/auth/api/authApi";
import { useAuthStore } from "@/stores/auth-store";

function SessionBootstrap() {
  const setSession = useAuthStore((s) => s.setSession);
  const clearSession = useAuthStore((s) => s.clearSession);

  useEffect(() => {
    // On app load there's no access token in memory yet (page refresh clears it by design),
    // but the httpOnly refresh cookie may still be valid — try it once before showing Login.
    authApi
      .refresh()
      .then((data) => setSession(data.accessToken, data.user))
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
