"use client";
import { useRouter } from "next/navigation";
import { useEffect } from "react";
// Customer report lives in the main Sales Reports page under the By Customer tab
export default function CustomersReportPage() {
  const router = useRouter();
  useEffect(() => { router.replace("/reports/sales?tab=by-customer"); }, [router]);
  return null;
}
