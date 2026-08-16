import Link from "next/link";
import { Handshake, LifeBuoy, FileText, CheckSquare, UserPlus, Plus, Calendar } from "lucide-react";
import { recentActivity, quickActions, upcomingEvents } from "../data/mockData";

const activityIcons: Record<string, React.ComponentType<{ className?: string }>> = {
  deal: Handshake,
  ticket: LifeBuoy,
  quotation: FileText,
  task: CheckSquare,
  customer: UserPlus,
};

export function ActivityFeed() {
  return (
    <div className="rounded-lg border bg-background p-5 shadow-sm">
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-sm font-semibold">Recent Activity</h2>
        <span className="rounded-full bg-accent px-2 py-0.5 text-xs text-muted-foreground">
          Demo data
        </span>
      </div>
      <ul className="space-y-4">
        {recentActivity.map((item) => {
          const Icon = activityIcons[item.type] ?? CheckSquare;
          return (
            <li key={item.id} className="flex items-start gap-3">
              <div className="mt-0.5 flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-accent">
                <Icon className="h-3.5 w-3.5 text-muted-foreground" />
              </div>
              <div className="min-w-0">
                <p className="text-sm leading-tight">{item.text}</p>
                <p className="mt-0.5 text-xs text-muted-foreground">{item.time}</p>
              </div>
            </li>
          );
        })}
      </ul>
    </div>
  );
}

export function QuickActions() {
  return (
    <div className="rounded-lg border bg-background p-5 shadow-sm">
      <h2 className="mb-4 text-sm font-semibold">Quick Actions</h2>
      <div className="grid grid-cols-2 gap-2">
        {quickActions.map((action) => (
          <Link
            key={action.label}
            href={action.href}
            className="flex items-center gap-2 rounded-md border px-3 py-2 text-sm hover:bg-accent"
          >
            <Plus className="h-3.5 w-3.5 text-primary" />
            {action.label}
          </Link>
        ))}
      </div>
    </div>
  );
}

export function CalendarWidget() {
  return (
    <div className="rounded-lg border bg-background p-5 shadow-sm">
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-sm font-semibold">Upcoming</h2>
        <Calendar className="h-4 w-4 text-muted-foreground" />
      </div>
      <ul className="space-y-3">
        {upcomingEvents.map((event) => (
          <li key={event.id} className="text-sm">
            <p className="font-medium leading-tight">{event.title}</p>
            <p className="text-xs text-muted-foreground">{event.time}</p>
          </li>
        ))}
      </ul>
    </div>
  );
}
