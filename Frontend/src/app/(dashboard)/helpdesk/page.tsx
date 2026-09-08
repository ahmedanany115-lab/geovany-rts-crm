"use client";
import { LifeBuoy } from "lucide-react";
export default function Page() {
  return (
    <div className="p-6 flex flex-col items-center justify-center min-h-[60vh] text-center space-y-4">
      <div className="w-16 h-16 rounded-2xl bg-primary/10 flex items-center justify-center">
        <LifeBuoy className="h-8 w-8 text-primary" />
      </div>
      <h1 className="text-2xl font-semibold">Help Desk</h1>
      <p className="text-muted-foreground max-w-sm">Customer support tickets and SLA management.</p>
      <span className="text-xs px-3 py-1 rounded-full bg-amber-100 text-amber-700 font-medium">Coming in next sprint</span>
    </div>
  );
}
