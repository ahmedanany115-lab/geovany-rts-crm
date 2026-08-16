"use client";

import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api-client";

export interface DashboardKpis {
  employeeCount: number;
  revenue: number;
  activeProjects: number;
  activeTickets: number;
}

export function useDashboardKpis() {
  return useQuery({
    queryKey: ["dashboard", "kpis"],
    queryFn: () => apiFetch<DashboardKpis>("/dashboard/kpis"),
    retry: 1,
  });
}
