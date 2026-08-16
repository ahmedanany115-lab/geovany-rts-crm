import { cn } from "@/lib/utils";
import type { LucideIcon } from "lucide-react";

interface KpiCardProps {
  label: string;
  value: string;
  delta?: string;
  deltaDirection?: "up" | "down";
  icon: LucideIcon;
}

export function KpiCard({ label, value, delta, deltaDirection, icon: Icon }: KpiCardProps) {
  return (
    <div className="rounded-lg border bg-background p-5 shadow-sm">
      <div className="flex items-center justify-between">
        <p className="text-sm text-muted-foreground">{label}</p>
        <div className="flex h-8 w-8 items-center justify-center rounded-md bg-primary/10 text-primary">
          <Icon className="h-4 w-4" />
        </div>
      </div>
      <p className="mt-3 text-2xl font-semibold">{value}</p>
      {delta && (
        <p
          className={cn(
            "mt-1 text-xs font-medium",
            deltaDirection === "up" ? "text-success" : "text-danger"
          )}
        >
          {deltaDirection === "up" ? "↑" : "↓"} {delta} vs last month
        </p>
      )}
    </div>
  );
}
