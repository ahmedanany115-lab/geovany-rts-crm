"use client";
import { useRouter } from "next/navigation";
import { useEffect } from "react";
// Revenue report lives in the main Sales Reports page under the Summary tab
export default function RevenueReportPage() {
  const router = useRouter();
  useEffect(() => { router.replace("/reports/sales"); }, [router]);
  return null;
}
