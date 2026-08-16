"use client";

import { Area, AreaChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts";
import { revenueTrend } from "../data/mockData";

export function RevenueChart() {
  return (
    <div className="rounded-lg border bg-background p-5 shadow-sm">
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-sm font-semibold">Revenue Trend</h2>
        <span className="rounded-full bg-accent px-2 py-0.5 text-xs text-muted-foreground">
          Demo data
        </span>
      </div>
      <ResponsiveContainer width="100%" height={220}>
        <AreaChart data={revenueTrend}>
          <defs>
            <linearGradient id="revenueFill" x1="0" y1="0" x2="0" y2="1">
              <stop offset="5%" stopColor="hsl(var(--primary))" stopOpacity={0.35} />
              <stop offset="95%" stopColor="hsl(var(--primary))" stopOpacity={0} />
            </linearGradient>
          </defs>
          <XAxis dataKey="month" axisLine={false} tickLine={false} fontSize={12} stroke="hsl(var(--muted-foreground))" />
          <YAxis
            axisLine={false}
            tickLine={false}
            fontSize={12}
            stroke="hsl(var(--muted-foreground))"
            tickFormatter={(v) => `$${v / 1000}k`}
          />
          <Tooltip
            formatter={(value: number) => [`$${value.toLocaleString()}`, "Revenue"]}
            contentStyle={{ borderRadius: 8, borderColor: "hsl(var(--border))", fontSize: 12 }}
          />
          <Area
            type="monotone"
            dataKey="revenue"
            stroke="hsl(var(--primary))"
            strokeWidth={2}
            fill="url(#revenueFill)"
          />
        </AreaChart>
      </ResponsiveContainer>
    </div>
  );
}
