"use client";
import { FileText } from "lucide-react";
export default function Page() {
  return (
    <div className="p-6 flex flex-col items-center justify-center min-h-[60vh] text-center space-y-4">
      <div className="w-16 h-16 rounded-2xl bg-primary/10 flex items-center justify-center">
        <FileText className="h-8 w-8 text-primary" />
      </div>
      <h1 className="text-2xl font-semibold">Quotations</h1>
      <p className="text-muted-foreground max-w-sm">Create and send price quotations to customers.</p>
      <span className="text-xs px-3 py-1 rounded-full bg-amber-100 text-amber-700 font-medium">Coming in next sprint</span>
    </div>
  );
}
