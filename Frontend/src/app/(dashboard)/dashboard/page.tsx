"use client";

import { DollarSign, Briefcase, LifeBuoy, Users, AlertCircle } from "lucide-react";
import { useDashboardKpis } from "@/features/dashboard/hooks/useDashboardKpis";
import { KpiCard } from "@/features/dashboard/components/KpiCard";
import { RevenueChart } from "@/features/dashboard/components/RevenueChart";
import { PipelineWidget } from "@/features/dashboard/components/PipelineWidget";
import { demoKpis } from "@/features/dashboard/data/mockData";
import {
  ActivityFeed,
  QuickActions,
  CalendarWidget,
} from "@/features/dashboard/components/DashboardWidgets";

function KpiSkeleton() {
  return (
    <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
      {Array.from({ length: 4 }).map((_, i) => (
        <div key={i} className="h-[104px] animate-pulse rounded-lg border bg-accent/40" />
      ))}
    </div>
  );
}

export default function DashboardPage() {
  const { data: kpis, isLoading, isError, refetch } = useDashboardKpis();

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Executive Dashboard</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          A live view of the business — revenue, pipeline, and what needs your attention.
        </p>
      </div>

      {isLoading && <KpiSkeleton />}

      {isError && (
        <div className="flex items-center gap-3 rounded-lg border border-danger/30 bg-danger/5 p-4 text-sm">
          <AlertCircle className="h-4 w-4 shrink-0 text-danger" />
          <span>Couldn&apos;t load KPI data.</span>
          <button onClick={() => refetch()} className="ml-auto font-medium text-primary hover:underline">
            Retry
          </button>
        </div>
      )}

      {kpis && (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <KpiCard
            label="Revenue (MTD)"
            value={`$${demoKpis.revenue.toLocaleString()}`}
            delta="12.4%"
            deltaDirection="up"
            icon={DollarSign}
          />
          <KpiCard label="Active Projects" value={String(demoKpis.activeProjects)} icon={Briefcase} />
          <KpiCard label="Active Tickets" value={String(demoKpis.activeTickets)} icon={LifeBuoy} />
          <KpiCard label="Employees" value={String(kpis.employeeCount)} icon={Users} />
        </div>
      )}

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
        <div className="lg:col-span-2">
          <RevenueChart />
        </div>
        <PipelineWidget />
      </div>

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
        <ActivityFeed />
        <QuickActions />
        <CalendarWidget />
      </div>
    </div>
  );
}
