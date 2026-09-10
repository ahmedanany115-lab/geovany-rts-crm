// Role-checking utilities for conditional UI rendering
"use client";
import { useAuthStore } from "@/stores/auth-store";

export function useRoles() {
  const user = useAuthStore((s) => s.user);
  const roles = user?.roles ?? [];

  const hasRole  = (...check: string[]) => check.some(r => roles.includes(r));
  const isAdmin  = hasRole("Admin");
  const isFinance = hasRole("Admin", "Accountant", "SalesManager");
  const isSales  = hasRole("Admin", "Manager", "SalesManager", "Sales");
  const canManageUsers = isAdmin;

  return { roles, hasRole, isAdmin, isFinance, isSales, canManageUsers };
}
