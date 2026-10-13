"use client";

import Link from "next/link";
import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api-client";
import { useAuthStore } from "@/stores/auth-store";
import { CalendarDays, ChevronLeft, ChevronRight } from "lucide-react";

type Meeting = {
  id: string;
  title: string;
  startTime: string;
  endTime: string;
  location?: string;
};

type Leave = {
  id: string;
  type: string;
  typeId: number;
  startDate: string;
  endDate: string;
  status: string;
  statusId: number;
};

function ymd(d: Date) {
  return d.toISOString().split("T")[0];
}

export function PersonalCalendar() {
  const user = useAuthStore(s => s.user);
  const [month, setMonth] = useState(() => new Date(new Date().getFullYear(), new Date().getMonth(), 1));

  const monthStart = new Date(month.getFullYear(), month.getMonth(), 1);
  const monthEnd = new Date(month.getFullYear(), month.getMonth() + 1, 0);
  const startKey = ymd(monthStart);
  const endKey = ymd(monthEnd);

  const { data: meetings } = useQuery({
    queryKey: ["dashboard-my-meetings", user?.id, startKey, endKey],
    queryFn: () => apiFetch<Meeting[]>(
      `/hr/meetings?employeeId=${user!.id}&fromDate=${startKey}&toDate=${endKey}T23:59:59`
    ),
    enabled: !!user?.id,
  });

  const { data: leaves } = useQuery({
    queryKey: ["dashboard-my-leaves", user?.id],
    queryFn: () => apiFetch<Leave[]>(`/hr/leaves?employeeId=${user!.id}`),
    enabled: !!user?.id,
  });

  const days = useMemo(() => {
    const firstWeekday = monthStart.getDay();
    const count = monthEnd.getDate();
    return [
      ...Array.from({ length: firstWeekday }, () => null),
      ...Array.from({ length: count }, (_, i) => i + 1),
    ];
  }, [month.getFullYear(), month.getMonth()]);

  const leaveForDay = (day: number) => {
    const key = ymd(new Date(month.getFullYear(), month.getMonth(), day));
    return (leaves ?? []).filter(l => l.statusId !== 3 && l.startDate <= key && l.endDate >= key);
  };

  const meetingsForDay = (day: number) => {
    const key = ymd(new Date(month.getFullYear(), month.getMonth(), day));
    return (meetings ?? []).filter(m => m.startTime?.slice(0, 10) === key);
  };

  const today = ymd(new Date());

  return (
    <div className="card overflow-hidden">
      <div className="px-5 py-3 border-b flex items-center justify-between gap-3">
        <div className="flex items-center gap-2">
          <CalendarDays className="h-4 w-4 text-primary" />
          <div>
            <h2 className="font-semibold">My Calendar</h2>
            <p className="text-xs text-muted-foreground">Meetings and leave requests</p>
          </div>
        </div>
        <div className="flex items-center gap-1">
          <button
            onClick={() => setMonth(new Date(month.getFullYear(), month.getMonth() - 1, 1))}
            className="p-1.5 rounded hover:bg-muted"
            aria-label="Previous month"
          >
            <ChevronLeft className="h-4 w-4" />
          </button>
          <span className="min-w-[140px] text-center text-sm font-medium">
            {month.toLocaleDateString("en-GB", { month: "long", year: "numeric" })}
          </span>
          <button
            onClick={() => setMonth(new Date(month.getFullYear(), month.getMonth() + 1, 1))}
            className="p-1.5 rounded hover:bg-muted"
            aria-label="Next month"
          >
            <ChevronRight className="h-4 w-4" />
          </button>
        </div>
      </div>

      <div className="p-4">
        <div className="grid grid-cols-7 text-[11px] text-muted-foreground font-medium mb-1">
          {["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"].map(d => (
            <div key={d} className="text-center py-1">{d}</div>
          ))}
        </div>

        <div className="grid grid-cols-7 border-l border-t">
          {days.map((day, i) => {
            if (!day) return <div key={i} className="min-h-[92px] border-r border-b bg-muted/10" />;

            const dateKey = ymd(new Date(month.getFullYear(), month.getMonth(), day));
            const dayMeetings = meetingsForDay(day);
            const dayLeaves = leaveForDay(day);
            const isToday = dateKey === today;

            return (
              <div key={i} className="min-h-[92px] border-r border-b p-1.5 overflow-hidden">
                <div className={`h-6 w-6 flex items-center justify-center rounded-full text-xs mb-1 ${isToday ? "bg-primary text-primary-foreground font-semibold" : ""}`}>
                  {day}
                </div>

                <div className="space-y-1">
                  {dayMeetings.slice(0, 2).map(m => (
                    <Link
                      key={m.id}
                      href="/hr/meetings"
                      title={m.title}
                      className="block truncate rounded px-1.5 py-0.5 text-[10px] bg-blue-50 text-blue-700 hover:bg-blue-100"
                    >
                      Meeting: {m.title}
                    </Link>
                  ))}
                  {dayLeaves.slice(0, 2).map(l => (
                    <Link
                      key={l.id}
                      href="/hr/leaves"
                      title={l.type}
                      className="block truncate rounded px-1.5 py-0.5 text-[10px] bg-amber-50 text-amber-700 hover:bg-amber-100"
                    >
                      Leave: {l.type}
                    </Link>
                  ))}
                  {dayMeetings.length + dayLeaves.length > 4 && (
                    <div className="text-[10px] text-muted-foreground px-1">
                      +{dayMeetings.length + dayLeaves.length - 4} more
                    </div>
                  )}
                </div>
              </div>
            );
          })}
        </div>

        <div className="flex flex-wrap gap-4 mt-3 text-xs text-muted-foreground">
          <span className="flex items-center gap-1.5"><span className="h-2.5 w-2.5 rounded-sm bg-blue-100 border border-blue-200" /> Meetings</span>
          <span className="flex items-center gap-1.5"><span className="h-2.5 w-2.5 rounded-sm bg-amber-100 border border-amber-200" /> Leave / Permission</span>
        </div>
      </div>
    </div>
  );
}
