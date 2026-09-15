"use client";
import { useEffect } from "react";
import { useRouter } from "next/navigation";

// The main dashboard redirects to the ERP dashboard (user-centric homepage)
export default function DashboardRedirect() {
  const router = useRouter();
  useEffect(() => { router.replace("/erp/dashboard"); }, [router]);
  return null;
}
